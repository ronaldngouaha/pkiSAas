using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            try
            {
                var result = await _auth.LoginAsync(dto, ip);
                if (result == null) return Unauthorized();
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            try
            {
                var result = await _auth.RefreshAsync(refreshToken, ip);
                if (result == null) return Unauthorized();
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] string refreshToken)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auth.RevokeRefreshTokenAsync(refreshToken, ip);
            return NoContent();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromQuery] Guid? tenantId, [FromBody] RegisterRequestDto dto)
        {
            try
            {
                var user = await _auth.RegisterAsync(tenantId, dto);
                return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("seed-superadmin")]
        public async Task<IActionResult> SeedSuperAdmin([FromBody] RegisterRequestDto dto)
        {
            await _auth.SeedSuperAdminAsync(dto);
            return Ok();
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
        }

        [Authorize(Policy = "SuperAdminOnly")]
        [HttpGet("introspect")]
        public IActionResult Introspect([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("Token is required.");
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                return Ok(new
                {
                    Header = jwt.Header,
                    Claims = jwt.Claims.Select(c => new { c.Type, c.Value }),
                    ValidFrom = jwt.ValidFrom,
                    ValidTo = jwt.ValidTo,
                    Issuer = jwt.Issuer,
                    Audience = jwt.Audiences
                });
            }
            catch
            {
                return BadRequest("Invalid token format.");
            }
        }
    }
}