using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Models;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    /// <summary>
    /// Endpoints d'authentification et gestion des tokens.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly TenantsIdentityDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IKeyProvider _keyProvider;

        public AuthController(
            IAuthService auth,
            TenantsIdentityDbContext db,
            IConfiguration configuration,
            IKeyProvider keyProvider)
        {
            _auth = auth;
            _db = db;
            _configuration = configuration;
            _keyProvider = keyProvider;
        }

        /// <summary>
        /// Lien pour connecter un utilisateur.
        /// </summary>
        /// <remarks>
        /// Retourne un access token JWT et un refresh token si les identifiants sont valides.
        /// Si MFA est active pour un compte SuperAdmin, il faut fournir mfaCode (TOTP) ou recoveryCode.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            try
            {
                var result = await _auth.LoginAsync(dto, ip);
                if (result == null)
                {
                    return BuildEnvelope(StatusCodes.Status401Unauthorized, null, "Identifiants invalides.");
                }

                return BuildEnvelope(StatusCodes.Status200OK, result, "Requete traitee avec succes.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return BuildEnvelope(StatusCodes.Status401Unauthorized, null, ex.Message);
            }
        }

        /// <summary>
        /// Lien pour renouveler la session avec un refresh token.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            try
            {
                var result = await _auth.RefreshAsync(refreshToken, ip);
                if (result == null)
                {
                    return BuildEnvelope(StatusCodes.Status401Unauthorized, null, "Refresh token invalide.");
                }

                return BuildEnvelope(StatusCodes.Status200OK, result, "Requete traitee avec succes.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return BuildEnvelope(StatusCodes.Status401Unauthorized, null, ex.Message);
            }
        }

        /// <summary>
        /// Lien pour revoquer un refresh token.
        /// </summary>
        [Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] string refreshToken)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auth.RevokeRefreshTokenAsync(refreshToken, ip);
            return BuildEnvelope(StatusCodes.Status200OK, null, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour enregistrer un nouvel utilisateur.
        /// </summary>
        /// <remarks>
        /// Cree un utilisateur pour un tenant donne, ou superadmin si tenantId est null.
        /// </remarks>
        [Authorize]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromQuery] Guid? tenantId, [FromBody] RegisterRequestDto dto)
        {
            if (!CanRegisterForTenant(tenantId, dto))
            {
                return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse.");
            }

            try
            {
                var user = await _auth.RegisterAsync(tenantId, dto);
                return BuildEnvelope(StatusCodes.Status201Created, user, "Requete traitee avec succes.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status409Conflict, null, ex.Message);
            }
        }

        /// <summary>
        /// Lien d'initialisation du premier compte SuperAdmin.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("seed-superadmin")]
        public async Task<IActionResult> SeedSuperAdmin([FromBody] RegisterRequestDto dto)
        {
            var hasSuperAdmin = await _db.Users.AnyAsync(u => u.Role == TenantRole.SuperAdmin && u.IsActive);
            if (hasSuperAdmin && !User.HasClaim(c => c.Type == "roles" && c.Value == "SuperAdmin"))
            {
                return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse.");
            }

            await _auth.SeedSuperAdminAsync(dto);
            return BuildEnvelope(StatusCodes.Status200OK, null, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour voir les claims JWT de l'utilisateur courant.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            return BuildEnvelope(StatusCodes.Status200OK, claims, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien d'inspection d'un JWT (debug/admin).
        /// </summary>
        /// <remarks>
        /// Decode le token et retourne son header, ses claims et ses dates de validite.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("introspect")]
        public async Task<IActionResult> Introspect([FromBody] IntrospectRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token))
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, "Token is required.");
            }

            try
            {
                var issuer = _configuration["Jwt:Issuer"]
                    ?? _configuration["JWT_ISSUER"]
                    ?? "Acme.Pki.Tenants.Identity";

                var audience = _configuration["Jwt:Audience"]
                    ?? _configuration["JWT_AUDIENCE"]
                    ?? "Acme.Pki";

                var jwks = await _keyProvider.GetPublicJwksAsync();
                var signingKeys = new JsonWebKeySet(jwks).GetSigningKeys();

                var handler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = signingKeys,
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "roles"
                };

                handler.ValidateToken(request.Token, validationParameters, out var validatedToken);
                var jwt = validatedToken as JwtSecurityToken ?? handler.ReadJwtToken(request.Token);
                var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var tid = jwt.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;

                Guid? userId = null;
                if (Guid.TryParse(sub, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                Guid? tokenTenantId = null;
                if (Guid.TryParse(tid, out var parsedTenantId))
                {
                    tokenTenantId = parsedTenantId;
                }

                string? email = null;
                Guid? dbTenantId = null;
                string[] roles;
                if (userId.HasValue)
                {
                    var user = await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);

                    email = user?.Email;
                    dbTenantId = user?.TenantId;
                    roles = user is null
                        ? Array.Empty<string>()
                        : UserRoleResolver.GetRoles(user)
                            .Select(r => r.ToString())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray();
                }
                else
                {
                    roles = Array.Empty<string>();
                }

                if (roles.Length == 0)
                {
                    roles = jwt.Claims
                        .Where(c => c.Type == "roles")
                        .Select(c => c.Value)
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }

                var expiresAtUtc = jwt.ValidTo;
                var remaining = expiresAtUtc - DateTime.UtcNow;
                var remainingValiditySeconds = remaining.TotalSeconds > 0
                    ? (int)Math.Floor(remaining.TotalSeconds)
                    : 0;

                return BuildEnvelope(StatusCodes.Status200OK, new
                {
                    UserId = userId,
                    TenantId = dbTenantId ?? tokenTenantId,
                    Email = email,
                    Role = roles,
                    RemainingValiditySeconds = remainingValiditySeconds,
                    ExpiresAtUtc = expiresAtUtc
                }, "Requete traitee avec succes.");
            }
            catch (SecurityTokenException)
            {
                return BuildEnvelope(StatusCodes.Status401Unauthorized, null, "Invalid token.");
            }
            catch
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, "Invalid token format.");
            }
        }

        public class IntrospectRequest
        {
            public string Token { get; set; }
        }

        private bool CanRegisterForTenant(Guid? tenantId, RegisterRequestDto dto)
        {
            if (tenantId == null)
            {
                return User.HasClaim(c => c.Type == "roles" && c.Value == "SuperAdmin");
            }

            var isSuperAdmin = User.HasClaim(c => c.Type == "roles" && c.Value == "SuperAdmin");
            if (isSuperAdmin)
            {
                return string.Equals(dto?.Role, "TenantAdmin", StringComparison.OrdinalIgnoreCase);
            }

            var canManageUsers = User.HasClaim(c => c.Type == "roles" && (c.Value == "TenantAdmin" || c.Value == "UserManager"));
            if (!canManageUsers)
            {
                return false;
            }

            var tid = User.FindFirst("tid")?.Value;
            if (!Guid.TryParse(tid, out var callerTenantId) || callerTenantId != tenantId.Value)
            {
                return false;
            }

            var requestedRole = dto?.Role?.Trim();
            if (!string.Equals(requestedRole, "TenantAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return User.HasClaim(c => (c.Type == "amr" && c.Value == "mfa") || (c.Type == "mfa" && c.Value == "true"));
        }

        private ObjectResult BuildEnvelope(int statusCode, object? data, string message)
        {
            return StatusCode(statusCode, new
            {
                statuscode = statusCode,
                data,
                message
            });
        }
    }
}