using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    /// <summary>
    /// Endpoints de gestion des domaines d'un tenant.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "TenantAdminPolicy")]
    [Route("api/v1/tenants/{tenantId:guid}/domains")]
    public class DomainsController : ControllerBase
    {
        private readonly IDomainService _domainService;

        public DomainsController(IDomainService domainService)
        {
            _domainService = domainService;
        }

        /// <summary>
        /// Lien pour ajouter un domaine a un tenant.
        /// </summary>
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Add(Guid tenantId, [FromBody] AddDomainRequest request)
        {
            await _domainService.AddDomainAsync(tenantId, request.Domain, request.ValidationMethod ?? "dns-txt");
            return BuildEnvelope(StatusCodes.Status202Accepted, null, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour valider la propriete d'un domaine.
        /// </summary>
        /// <remarks>
        /// Verifie la reponse de challenge (ex: DNS TXT) et marque le domaine comme valide.
        /// </remarks>
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost("validate")]
        public async Task<IActionResult> Validate(Guid tenantId, [FromBody] ValidateDomainRequest request)
        {
            var ok = await _domainService.ValidateDomainAsync(tenantId, request.Domain, request.ChallengeResponse);
            if (!ok)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, "Domain validation failed.");
            }

            return BuildEnvelope(StatusCodes.Status200OK, new { validated = true }, "Requete traitee avec succes.");
        }

        public class AddDomainRequest
        {
            public string Domain { get; set; }
            public string ValidationMethod { get; set; }
        }

        public class ValidateDomainRequest
        {
            public string Domain { get; set; }
            public string ChallengeResponse { get; set; }
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
