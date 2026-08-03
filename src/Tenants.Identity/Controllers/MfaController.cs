using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Acme.Pki.Tenants.Identity.Services;
using Acme.Pki.Tenants.Identity.DTOs.Mfa;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    /// <summary>
    /// Endpoints MFA (TOTP + codes de recuperation).
    /// </summary>
    [ApiController]
    [Authorize(Policy = "EndUserOwnResourcePolicy")]
    [Route("api/v1/mfa")]
    public class MfaController : ControllerBase
    {
        private readonly IMfaService _mfa;

        public MfaController(IMfaService mfa)
        {
            _mfa = mfa;
        }

        /// <summary>
        /// Lien pour demarrer l'activation MFA TOTP.
        /// </summary>
        /// <remarks>
        /// Retourne une image PNG du QR code a scanner dans Microsoft/Google Authenticator.
        /// La cle manuelle est exposee dans l'en-tete X-Manual-Entry-Key.
        /// </remarks>
        /// <param name="userId">Identifiant de l'utilisateur cible.</param>
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [HttpPost("{userId:guid}/totp/begin")]
        public async Task<IActionResult> BeginTotp(Guid userId)
        {
            var setup = await _mfa.BeginTotpSetupAsync(userId);
            return BuildEnvelope(StatusCodes.Status200OK, new
            {
                manualEntryKey = setup.ManualEntryKey,
                qrCodePngBase64 = Convert.ToBase64String(setup.QrCodePng)
            }, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour verifier le code TOTP saisi par l'utilisateur.
        /// </summary>
        /// <remarks>
        /// A appeler juste apres la configuration MFA pour confirmer l'activation.
        /// </remarks>
        /// <param name="userId">Identifiant de l'utilisateur cible.</param>
        /// <param name="dto">Code OTP a 6 chiffres.</param>
        [HttpPost("{userId:guid}/totp/verify")]
        public async Task<IActionResult> VerifyTotp(Guid userId, [FromBody] MfaVerifyDto dto)
        {
            var ok = await _mfa.VerifyTotpAsync(userId, dto.Code);
            if (!ok)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, "Invalid code");
            }

            return BuildEnvelope(StatusCodes.Status200OK, new { verified = true }, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour generer des codes de recuperation MFA.
        /// </summary>
        /// <remarks>
        /// Ces codes servent de secours si l'utilisateur perd l'application d'authentification.
        /// </remarks>
        /// <param name="userId">Identifiant de l'utilisateur cible.</param>
        [HttpPost("{userId:guid}/recovery/generate")]
        public async Task<IActionResult> GenerateRecovery(Guid userId)
        {
            var codes = await _mfa.GenerateRecoveryCodesAsync(userId);
            return BuildEnvelope(StatusCodes.Status200OK, codes, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour consommer un code de recuperation.
        /// </summary>
        /// <remarks>
        /// Un code utilise est marque comme consomme et ne peut plus etre reutilise.
        /// </remarks>
        /// <param name="userId">Identifiant de l'utilisateur cible.</param>
        /// <param name="dto">Code de recuperation saisi par l'utilisateur.</param>
        [HttpPost("{userId:guid}/recovery/consume")]
        public async Task<IActionResult> ConsumeRecovery(Guid userId, [FromBody] MfaVerifyDto dto)
        {
            var ok = await _mfa.ConsumeRecoveryCodeAsync(userId, dto.Code);
            if (!ok)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, "Invalid recovery code");
            }

            return BuildEnvelope(StatusCodes.Status200OK, new { consumed = true }, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour desactiver MFA TOTP.
        /// </summary>
        /// <remarks>
        /// Desactive MFA sur le compte et revoque les secrets TOTP actifs.
        /// </remarks>
        /// <param name="userId">Identifiant de l'utilisateur cible.</param>
        [HttpPost("{userId:guid}/totp/disable")]
        public async Task<IActionResult> DisableTotp(Guid userId)
        {
            await _mfa.DisableTotpAsync(userId);
            return BuildEnvelope(StatusCodes.Status200OK, null, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour consulter le statut MFA de l'utilisateur.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur cible.</param>
        [HttpGet("{userId:guid}/status")]
        public async Task<IActionResult> Status(Guid userId)
        {
            var enabled = await _mfa.IsMfaEnabledAsync(userId);
            return BuildEnvelope(StatusCodes.Status200OK, new { mfaEnabled = enabled }, "Requete traitee avec succes.");
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
