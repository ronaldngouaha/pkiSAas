using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly TenantsIdentityDbContext _db;
        private readonly IKeyProvider _keyProvider;
        private readonly IMfaService _mfaService;
        private readonly IKeyManagementService? _keyManagementService;
        private readonly IConfiguration _configuration;
        private readonly IIdentityTelemetry _telemetry;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            TenantsIdentityDbContext db,
            IKeyProvider keyProvider,
            IMfaService mfaService,
            IConfiguration configuration,
            IIdentityTelemetry telemetry,
            ILogger<AuthService> logger,
            IKeyManagementService? keyManagementService = null)
        {
            _db = db;
            _keyProvider = keyProvider;
            _mfaService = mfaService;
            _configuration = configuration;
            _telemetry = telemetry;
            _logger = logger;
            _keyManagementService = keyManagementService;
        }

        public async Task<AuthResultDto> LoginAsync(LoginRequestDto dto, string ip)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                _telemetry.RecordLoginFailure("missing_credentials");
                _logger.LogWarning("auth.login.failed reason={Reason}", "missing_credentials");
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            if (user == null)
            {
                _telemetry.RecordLoginFailure("user_not_found");
                _logger.LogWarning("auth.login.failed reason={Reason}", "user_not_found");
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            if (!user.IsActive)
            {
                _telemetry.RecordLoginFailure("user_inactive");
                _logger.LogWarning("auth.login.failed reason={Reason} userId={UserId}", "user_inactive", user.Id);
                throw new UnauthorizedAccessException("Desactivated account.");
            }

            if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.UtcNow)
            {
                _telemetry.RecordLoginFailure("account_locked");
                _logger.LogWarning("auth.login.failed reason={Reason} userId={UserId}", "account_locked", user.Id);
                throw new UnauthorizedAccessException("Account is locked.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                user.FailedLoginCount += 1;
                var maxFailed = _configuration.GetValue<int?>("Auth:Lockout:MaxFailedAttempts") ?? 5;
                if (user.FailedLoginCount >= maxFailed)
                {
                    var lockoutMinutes = _configuration.GetValue<int?>("Auth:Lockout:LockoutMinutes") ?? 15;
                    user.LockoutUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                }

                await _db.SaveChangesAsync();
                _telemetry.RecordLoginFailure("invalid_password");
                _logger.LogWarning("auth.login.failed reason={Reason} userId={UserId}", "invalid_password", user.Id);
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            if (user.MfaEnabled)
            {
                var hasTotpCode = !string.IsNullOrWhiteSpace(dto.MfaCode);
                var hasRecoveryCode = !string.IsNullOrWhiteSpace(dto.RecoveryCode);

                if (!hasTotpCode && !hasRecoveryCode)
                {
                    _telemetry.RecordMfaFailure("mfa_code_required");
                    _telemetry.RecordLoginFailure("mfa_code_required");
                    _logger.LogWarning("auth.mfa.failed reason={Reason} userId={UserId}", "mfa_code_required", user.Id);
                    throw new UnauthorizedAccessException("MFA code required.");
                }

                var isMfaValid = false;

                if (hasTotpCode)
                {
                    isMfaValid = await _mfaService.VerifyTotpAsync(user.Id, dto.MfaCode!.Trim());
                }

                if (!isMfaValid && hasRecoveryCode)
                {
                    isMfaValid = await _mfaService.ConsumeRecoveryCodeAsync(user.Id, dto.RecoveryCode!.Trim());
                }

                if (!isMfaValid)
                {
                    _telemetry.RecordMfaFailure("invalid_mfa_code");
                    _telemetry.RecordLoginFailure("invalid_mfa_code");
                    _logger.LogWarning("auth.mfa.failed reason={Reason} userId={UserId}", "invalid_mfa_code", user.Id);
                    throw new UnauthorizedAccessException("Invalid MFA code.");
                }
            }

            var mfaSatisfiedForToken = user.MfaEnabled;

            user.FailedLoginCount = 0;
            user.LockoutUntil = null;
            user.LastLoginAt = DateTime.UtcNow;

            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenMinutes());
            var accessToken = await BuildAccessTokenAsync(user, accessTokenExpiresAt, mfaSatisfiedForToken);

            var rawRefreshToken = GenerateRefreshToken();
            var refreshTokenHash = HashRefreshToken(rawRefreshToken);
            var refreshTtlDays = GetRefreshTokenDays();
            var refreshExpiresAt = DateTime.UtcNow.AddDays(refreshTtlDays);

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = refreshExpiresAt,
                CreatedByIp = ip
            });

            await _db.SaveChangesAsync();
            _telemetry.RecordLoginSuccess(user.MfaEnabled);
            _logger.LogInformation("auth.login.success userId={UserId} tenantId={TenantId} mfaEnabled={MfaEnabled}", user.Id, user.TenantId, user.MfaEnabled);

            return new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt
            };
        }

        public async Task<AuthResultDto> RefreshAsync(string refreshToken, string ip)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _telemetry.RecordRefreshFailure("missing_refresh_token");
                _logger.LogWarning("auth.refresh.failed reason={Reason}", "missing_refresh_token");
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            var refreshTokenHash = HashRefreshToken(refreshToken);
            var tokenEntity = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.TokenHash == refreshTokenHash);

            if (tokenEntity == null)
            {
                _telemetry.RecordRefreshFailure("refresh_token_not_found");
                _logger.LogWarning("auth.refresh.failed reason={Reason}", "refresh_token_not_found");
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            if (tokenEntity.RevokedAt.HasValue)
            {
                _telemetry.RecordTokenReplayAttempt();
                _telemetry.RecordRefreshFailure("refresh_token_replayed");
                _logger.LogWarning("auth.refresh.replay_attempt tokenId={TokenId} userId={UserId}", tokenEntity.Id, tokenEntity.UserId);
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            if (tokenEntity.ExpiresAt <= DateTime.UtcNow)
            {
                _telemetry.RecordRefreshFailure("refresh_token_expired");
                _logger.LogWarning("auth.refresh.failed reason={Reason} tokenId={TokenId} userId={UserId}", "refresh_token_expired", tokenEntity.Id, tokenEntity.UserId);
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == tokenEntity.UserId);
            if (user == null || !user.IsActive)
            {
                _telemetry.RecordRefreshFailure("refresh_token_user_invalid");
                _logger.LogWarning("auth.refresh.failed reason={Reason} userId={UserId}", "refresh_token_user_invalid", tokenEntity.UserId);
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            var newRawRefreshToken = GenerateRefreshToken();
            var newRefreshTokenHash = HashRefreshToken(newRawRefreshToken);

            tokenEntity.RevokedAt = DateTime.UtcNow;
            tokenEntity.RevokedByIp = ip;
            tokenEntity.ReplacedByTokenHash = newRefreshTokenHash;

            var refreshTtlDays = GetRefreshTokenDays();
            var refreshExpiresAt = DateTime.UtcNow.AddDays(refreshTtlDays);
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
                ExpiresAt = refreshExpiresAt,
                CreatedByIp = ip
            });

            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenMinutes());
            var accessToken = await BuildAccessTokenAsync(user, accessTokenExpiresAt, user.MfaEnabled);

            await _db.SaveChangesAsync();
            _logger.LogInformation("auth.refresh.success userId={UserId} tenantId={TenantId}", user.Id, user.TenantId);

            return new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = newRawRefreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, string ip)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var refreshTokenHash = HashRefreshToken(refreshToken);
            var tokenEntity = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == refreshTokenHash);
            if (tokenEntity == null || tokenEntity.RevokedAt.HasValue)
            {
                return;
            }

            tokenEntity.RevokedAt = DateTime.UtcNow;
            tokenEntity.RevokedByIp = ip;
            await _db.SaveChangesAsync();
        }

        public async Task<UserDto> RegisterAsync(Guid? tenantId, RegisterRequestDto dto)
        {
            var email = dto.Email.Trim();
            var normalizedEmail = email.ToLowerInvariant();

            var exists = await _db.Users.AnyAsync(u => u.TenantId == tenantId && u.NormalizedEmail == normalizedEmail);
            if (exists)
            {
                throw new InvalidOperationException("User already exists for this tenant.");
            }

            var role = ResolveRole(tenantId, dto.Role);
            var user = new User
            {
                TenantId = tenantId,
                Email = email,
                NormalizedEmail = normalizedEmail,
                DisplayName = dto.DisplayName,
                Username = normalizedEmail,
                Role = role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsEmailVerified = false,
                EmailVerificationTokenHash = string.Empty,
                MfaEnabled = false,
                MfaMethods = "[]",
                IsActive = true,
                FailedLoginCount = 0,
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                IsPhoneVerified = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ServiceAccount = role == TenantRole.ServiceAccount,
                ConsentVersion = "v1"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return MapUser(user);
        }

        public async Task SeedSuperAdminAsync(RegisterRequestDto dto)
        {
            var hasSuperAdmin = await _db.Users.AnyAsync(u => u.Role == TenantRole.SuperAdmin);
            if (hasSuperAdmin)
            {
                return;
            }

            await RegisterAsync(null, new RegisterRequestDto
            {
                Email = dto.Email,
                DisplayName = dto.DisplayName,
                Password = dto.Password,
                Role = TenantRole.SuperAdmin.ToString()
            });
        }

        public async Task<bool> ValidatePasswordAsync(string email, string password)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        private async Task<string> BuildAccessTokenAsync(User user, DateTime expiresAt, bool mfaSatisfied = false)
        {
            var issuer = ResolveConfigValue("Jwt:Issuer", "JWT_ISSUER") ?? "Acme.Pki.Tenants.Identity";
            var audience = ResolveConfigValue("Jwt:Audience", "JWT_AUDIENCE") ?? "Acme.Pki";

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("tid", user.TenantId?.ToString() ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("metadata", UserRoleResolver.BuildPublicMetadata(user.Metadata))
            };

            foreach (var role in UserRoleResolver.GetRoles(user).Select(r => r.ToString()))
            {
                claims.Add(new Claim("roles", role));
            }

            if (mfaSatisfied)
            {
                claims.Add(new Claim("amr", "mfa"));
                claims.Add(new Claim("mfa", "true"));
            }

            var (keyId, privateKey) = await ResolveActiveSigningKeyAsync();
            var rsaSecurityKey = new RsaSecurityKey(privateKey) { KeyId = keyId };
            var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int GetAccessTokenMinutes()
        {
            if (int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var fromJwt)) return fromJwt;
            if (int.TryParse(_configuration["JWT_ACCESS_MINUTES"], out var fromEnv)) return fromEnv;
            return 30;
        }

        private string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private int GetRefreshTokenDays()
        {
            if (int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var fromJwt)) return fromJwt;
            if (int.TryParse(_configuration["JWT_REFRESH_DAYS"], out var fromEnv)) return fromEnv;
            return 30;
        }

        private string HashRefreshToken(string token)
        {
            var hashKey = _configuration["Auth:RefreshTokenHashKey"];
            if (string.IsNullOrWhiteSpace(hashKey))
            {
                throw new InvalidOperationException("Missing Auth:RefreshTokenHashKey configuration.");
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hashKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash);
        }

        private string? ResolveConfigValue(string configKey, string envKey)
        {
            var fromConfig = _configuration[configKey];
            if (!string.IsNullOrWhiteSpace(fromConfig) && !(fromConfig.StartsWith("${") && fromConfig.EndsWith("}")))
            {
                return fromConfig;
            }

            var fromEnv = _configuration[envKey] ?? Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrWhiteSpace(fromEnv) && !(fromEnv.StartsWith("${") && fromEnv.EndsWith("}")))
            {
                return fromEnv;
            }

            return null;
        }

        private static TenantRole ResolveRole(Guid? tenantId, string? requestedRole)
        {
            if (tenantId == null)
            {
                return TenantRole.SuperAdmin;
            }

            if (string.IsNullOrWhiteSpace(requestedRole))
            {
                return TenantRole.User;
            }

            if (Enum.TryParse<TenantRole>(requestedRole, true, out var parsedRole))
            {
                return parsedRole == TenantRole.SuperAdmin ? TenantRole.TenantAdmin : parsedRole;
            }

            return TenantRole.User;
        }

        private static UserDto MapUser(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Email = user.Email,
                NormalizedEmail = user.NormalizedEmail,
                DisplayName = user.DisplayName,
                Role = UserRoleResolver.GetRoles(user).Select(r => r.ToString()).ToArray(),
                IsEmailVerified = user.IsEmailVerified,
                MfaEnabled = user.MfaEnabled,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
                ,
                Metadata = UserRoleResolver.BuildPublicMetadata(user.Metadata)
            };
        }

        private async Task<(string KeyId, RSAParameters PrivateKey)> ResolveActiveSigningKeyAsync()
        {
            if (_keyManagementService != null)
            {
                return await _keyManagementService.GetActiveSigningKeyAsync();
            }

            return await _keyProvider.GetActiveRsaKeyAsync();
        }
    }
}