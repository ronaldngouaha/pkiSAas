using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.DTOs.Mfa;
using Acme.Pki.Tenants.Identity.Models;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class AuthServiceTests
    {
        private static TenantsIdentityDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<TenantsIdentityDbContext>()
                .UseInMemoryDatabase(databaseName: $"AuthServiceDb-{Guid.NewGuid()}")
                .Options;
            return new TenantsIdentityDbContext(options);
        }

        private static IConfiguration CreateConfig()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "http://localhost",
                    ["Jwt:Audience"] = "pki-api",
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["Jwt:RefreshTokenDays"] = "30",
                    ["Auth:RefreshTokenHashKey"] = "unit-test-refresh-hash-key",
                    ["Auth:Lockout:MaxFailedAttempts"] = "5",
                    ["Auth:Lockout:LockoutMinutes"] = "15"
                })
                .Build();
        }

        private static AuthService CreateService(TenantsIdentityDbContext db)
        {
            var config = CreateConfig();
            var keyProvider = new FakeKeyProvider();
            var mfaService = new FakeMfaService();
            var telemetry = new IdentityTelemetry();
            return new AuthService(db, keyProvider, mfaService, config, telemetry, NullLogger<AuthService>.Instance);
        }

        [Fact]
        public async Task Login_Success_ShouldReturnTokens()
        {
            using var db = CreateDbContext();
            db.Users.Add(new User
            {
                TenantId = Guid.NewGuid(),
                Email = "user@test.local",
                NormalizedEmail = "user@test.local",
                DisplayName = "User",
                Username = "user@test.local",
                Role = TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.LoginAsync(new LoginRequestDto
            {
                Email = "user@test.local",
                Password = "P@ssw0rd!"
            }, "127.0.0.1");

            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
            Assert.True(result.RefreshTokenExpiresAt > DateTime.UtcNow);
            Assert.Single(db.RefreshTokens);
        }

        [Fact]
        public async Task Login_Failure_ShouldThrowUnauthorizedAccessException()
        {
            using var db = CreateDbContext();
            db.Users.Add(new User
            {
                TenantId = Guid.NewGuid(),
                Email = "user@test.local",
                NormalizedEmail = "user@test.local",
                DisplayName = "User",
                Username = "user@test.local",
                Role = TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("GoodPassword"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync(new LoginRequestDto
                {
                    Email = "user@test.local",
                    Password = "BadPassword"
                }, "127.0.0.1"));
        }

        [Fact]
        public async Task Login_InactiveUser_ShouldThrowDesactivatedAccountMessage()
        {
            using var db = CreateDbContext();
            db.Users.Add(new User
            {
                TenantId = Guid.NewGuid(),
                Email = "inactive@test.local",
                NormalizedEmail = "inactive@test.local",
                DisplayName = "Inactive",
                Username = "inactive@test.local",
                Role = TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = false
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync(new LoginRequestDto
                {
                    Email = "inactive@test.local",
                    Password = "P@ssw0rd!"
                }, "127.0.0.1"));

            Assert.Equal("Desactivated account.", ex.Message);
        }

        [Fact]
        public async Task Refresh_ShouldRotateTokens()
        {
            using var db = CreateDbContext();
            db.Users.Add(new User
            {
                TenantId = Guid.NewGuid(),
                Email = "user@test.local",
                NormalizedEmail = "user@test.local",
                DisplayName = "User",
                Username = "user@test.local",
                Role = TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("RotatePass"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var loginResult = await service.LoginAsync(new LoginRequestDto
            {
                Email = "user@test.local",
                Password = "RotatePass"
            }, "127.0.0.1");

            var refreshed = await service.RefreshAsync(loginResult.RefreshToken, "127.0.0.2");

            Assert.NotEqual(loginResult.RefreshToken, refreshed.RefreshToken);
            Assert.Equal(2, db.RefreshTokens.Count());
            Assert.Single(db.RefreshTokens.Where(r => !r.RevokedAt.HasValue));
            Assert.Single(db.RefreshTokens.Where(r => r.RevokedAt.HasValue));
        }

        [Fact]
        public async Task Revoke_ShouldMarkRefreshTokenAsRevoked()
        {
            using var db = CreateDbContext();
            db.Users.Add(new User
            {
                TenantId = Guid.NewGuid(),
                Email = "user@test.local",
                NormalizedEmail = "user@test.local",
                DisplayName = "User",
                Username = "user@test.local",
                Role = TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("RevokePass"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var loginResult = await service.LoginAsync(new LoginRequestDto
            {
                Email = "user@test.local",
                Password = "RevokePass"
            }, "127.0.0.1");

            await service.RevokeRefreshTokenAsync(loginResult.RefreshToken, "127.0.0.3");

            var token = db.RefreshTokens.Single();
            Assert.True(token.RevokedAt.HasValue);
            Assert.Equal("127.0.0.3", token.RevokedByIp);
        }

        [Fact]
        public async Task SeedSuperAdmin_ShouldCreateOnlyOneSuperAdmin()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var dto = new RegisterRequestDto
            {
                Email = "root@test.local",
                DisplayName = "Root",
                Password = "RootPass123!",
                Role = "SuperAdmin"
            };

            await service.SeedSuperAdminAsync(dto);
            await service.SeedSuperAdminAsync(dto);

            var superAdmins = db.Users.Where(u => u.Role == TenantRole.SuperAdmin).ToList();
            Assert.Single(superAdmins);
            Assert.Null(superAdmins[0].TenantId);
        }

        [Fact]
        public async Task Login_SuperAdmin_WithMfaEnabled_WithoutCode_ShouldThrowUnauthorizedAccessException()
        {
            using var db = CreateDbContext();
            db.Users.Add(new User
            {
                TenantId = null,
                Email = "root@test.local",
                NormalizedEmail = "root@test.local",
                DisplayName = "Root",
                Username = "root@test.local",
                Role = TenantRole.SuperAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("RootPass123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                MfaEnabled = true,
                IsActive = true
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync(new LoginRequestDto
                {
                    Email = "root@test.local",
                    Password = "RootPass123!"
                }, "127.0.0.1"));
        }

        [Fact]
        public async Task Login_SuperAdmin_WithMfaEnabled_WithTotp_ShouldSucceed()
        {
            using var db = CreateDbContext();
            db.Users.Add(new User
            {
                TenantId = null,
                Email = "root@test.local",
                NormalizedEmail = "root@test.local",
                DisplayName = "Root",
                Username = "root@test.local",
                Role = TenantRole.SuperAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("RootPass123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                MfaEnabled = true,
                IsActive = true
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.LoginAsync(new LoginRequestDto
            {
                Email = "root@test.local",
                Password = "RootPass123!",
                MfaCode = "123456"
            }, "127.0.0.1");

            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        }

        [Fact]
        public async Task Login_TenantUser_WithMfaEnabled_WithoutCode_ShouldThrowUnauthorizedAccessException()
        {
            using var db = CreateDbContext();
            db.Users.Add(new User
            {
                TenantId = Guid.NewGuid(),
                Email = "user@test.local",
                NormalizedEmail = "user@test.local",
                DisplayName = "User",
                Username = "user@test.local",
                Role = TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("UserPass123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                MfaEnabled = true,
                IsActive = true
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync(new LoginRequestDto
                {
                    Email = "user@test.local",
                    Password = "UserPass123!"
                }, "127.0.0.1"));
        }

        [Fact]
        public async Task Login_TenantUser_WithMfaEnabled_WithTotp_ShouldSucceed()
        {
            using var db = CreateDbContext();
            db.Users.Add(new User
            {
                TenantId = Guid.NewGuid(),
                Email = "user@test.local",
                NormalizedEmail = "user@test.local",
                DisplayName = "User",
                Username = "user@test.local",
                Role = TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("UserPass123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                MfaEnabled = true,
                IsActive = true
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.LoginAsync(new LoginRequestDto
            {
                Email = "user@test.local",
                Password = "UserPass123!",
                MfaCode = "123456"
            }, "127.0.0.1");

            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        }

        private sealed class FakeKeyProvider : IKeyProvider
        {
            private readonly (string KeyId, RSAParameters PrivateKey) _key;

            public FakeKeyProvider()
            {
                using var rsa = RSA.Create(2048);
                _key = ("test-kid", rsa.ExportParameters(true));
            }

            public Task<(string KeyId, RSAParameters PrivateKey)> GetActiveRsaKeyAsync()
            {
                return Task.FromResult(_key);
            }

            public Task<string> GetPublicJwksAsync()
            {
                using var rsa = RSA.Create();
                rsa.ImportParameters(_key.PrivateKey);
                var pub = rsa.ExportParameters(false);
                var n = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(pub.Modulus ?? Array.Empty<byte>());
                var e = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(pub.Exponent ?? Array.Empty<byte>());
                var jwks = $"{{\"keys\":[{{\"kty\":\"RSA\",\"use\":\"sig\",\"alg\":\"RS256\",\"kid\":\"{_key.KeyId}\",\"n\":\"{n}\",\"e\":\"{e}\"}}]}}";
                return Task.FromResult(jwks);
            }
        }

        private sealed class FakeMfaService : IMfaService
        {
            public Task<MfaSetupDto> BeginTotpSetupAsync(Guid userId)
            {
                return Task.FromResult(new MfaSetupDto
                {
                    ManualEntryKey = "TESTKEY",
                    QrCodePng = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
                });
            }

            public Task<bool> VerifyTotpAsync(Guid userId, string code)
            {
                return Task.FromResult(code == "123456");
            }

            public Task<string[]> GenerateRecoveryCodesAsync(Guid userId, int count = 10)
            {
                return Task.FromResult(new[] { "RECOVERY1" });
            }

            public Task<bool> ConsumeRecoveryCodeAsync(Guid userId, string code)
            {
                return Task.FromResult(code == "RECOVERY1");
            }

            public Task DisableTotpAsync(Guid userId)
            {
                return Task.CompletedTask;
            }

            public Task<bool> IsMfaEnabledAsync(Guid userId)
            {
                return Task.FromResult(true);
            }
        }
    }
}
