using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Acme.Pki.Tenants.Identity.Services;
using Acme.Pki.Tenants.Identity.DTOs.Mfa;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/mfa")]
    public class MfaController : ControllerBase
    {
        private readonly IMfaService _mfa;

        public MfaController(IMfaService mfa)
        {
            _mfa = mfa;
        }

        [HttpPost("{userId:guid}/totp/begin")]
        public async Task<IActionResult> BeginTotp(Guid userId)
        {
            var setup = await _mfa.BeginTotpSetupAsync(userId);
            return Ok(setup);
        }

        [HttpPost("{userId:guid}/totp/verify")]
        public async Task<IActionResult> VerifyTotp(Guid userId, [FromBody] MfaVerifyDto dto)
        {
            var ok = await _mfa.VerifyTotpAsync(userId, dto.Code);
            if (!ok)
            {
                return BadRequest(new { message = "Invalid code" });
            }

            return Ok();
        }

        [HttpPost("{userId:guid}/recovery/generate")]
        public async Task<IActionResult> GenerateRecovery(Guid userId)
        {
            var codes = await _mfa.GenerateRecoveryCodesAsync(userId);
            return Ok(codes);
        }

        [HttpPost("{userId:guid}/recovery/consume")]
        public async Task<IActionResult> ConsumeRecovery(Guid userId, [FromBody] MfaVerifyDto dto)
        {
            var ok = await _mfa.ConsumeRecoveryCodeAsync(userId, dto.Code);
            if (!ok)
            {
                return BadRequest(new { message = "Invalid recovery code" });
            }

            return Ok();
        }

        [HttpPost("{userId:guid}/totp/disable")]
        public async Task<IActionResult> DisableTotp(Guid userId)
        {
            await _mfa.DisableTotpAsync(userId);
            return NoContent();
        }

        [HttpGet("{userId:guid}/status")]
        public async Task<IActionResult> Status(Guid userId)
        {
            var enabled = await _mfa.IsMfaEnabledAsync(userId);
            return Ok(new { mfaEnabled = enabled });
        }
    }
}
