using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.DTOs.Domain;
using Acme.Pki.Tenants.Identity.Services;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    /// <summary>
    /// Endpoints de gestion des domaines d'un tenant.
    /// </summary>
    [ApiController]
    [Route("api/v1/tenants/{tenantId:guid}/domains")]
    public class DomainsController : ControllerBase
    {
        private readonly IDomainService _domain;

        public DomainsController(IDomainService domain) => _domain = domain;

        /// <summary>
        /// Lien pour ajouter un domaine a un tenant.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "TenantAdminPolicy")]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Add(Guid tenantId, [FromBody] TenantDomainCreateDto dto)
        {
            try
            {
                var created = await _domain.AddDomainAsync(tenantId, dto);
                return BuildEnvelope(StatusCodes.Status201Created, created, "Request processed successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status409Conflict, null, ex.Message);
            }
        }

        /// <summary>
        /// Lien pour recuperer un domaine par son identifiant.
        /// </summary>
        [HttpGet("{domainId:guid}")]
        [Authorize(Policy = "TenantAdminPolicy")]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(Guid tenantId, Guid domainId)
        {
            var d = await _domain.GetDomainAsync(domainId);
            if (d == null) return BuildEnvelope(StatusCodes.Status404NotFound, null, "Domain not found.");
            if (d.TenantId != tenantId) return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Access denied.");
            return BuildEnvelope(StatusCodes.Status200OK, d, "Request processed successfully.");
        }

        /// <summary>
        /// Lien pour generer le challenge DNS TXT d'un domaine.
        /// </summary>
        [HttpPost("{domainId:guid}/generate-dns")]
        [Authorize(Policy = "TenantAdminPolicy")]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GenerateDns(Guid tenantId, Guid domainId)
        {
            var domain = await _domain.GetDomainAsync(domainId);
            if (domain == null) return BuildEnvelope(StatusCodes.Status404NotFound, null, "Domain not found.");
            if (domain.TenantId != tenantId) return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Access denied.");

            var token = await _domain.GenerateDnsChallengeAsync(domainId);
            return BuildEnvelope(StatusCodes.Status200OK, new
            {
                challenge = token,
                record = $"_acme-challenge.{domain.Domain} TXT {token}"
            }, "Request processed successfully.");
        }

        /// <summary>
        /// Lien pour generer le challenge HTTP d'un domaine.
        /// </summary>
        [HttpPost("{domainId:guid}/generate-http")]
        [Authorize(Policy = "TenantAdminPolicy")]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GenerateHttp(Guid tenantId, Guid domainId)
        {
            var domain = await _domain.GetDomainAsync(domainId);
            if (domain == null) return BuildEnvelope(StatusCodes.Status404NotFound, null, "Domain not found.");
            if (domain.TenantId != tenantId) return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Access denied.");

            var token = await _domain.GenerateHttpChallengeAsync(domainId);
            return BuildEnvelope(StatusCodes.Status200OK, new
            {
                challenge = token,
                url = $"http://{domain.Domain}/.well-known/acme-challenge/{token}"
            }, "Request processed successfully.");
        }

        /// <summary>
        /// Lien pour valider la propriete d'un domaine.
        /// </summary>
        [HttpPost("{domainId:guid}/validate")]
        [Authorize(Policy = "TenantAdminPolicy")]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Validate(Guid tenantId, Guid domainId)
        {
            var domain = await _domain.GetDomainAsync(domainId);
            if (domain == null) return BuildEnvelope(StatusCodes.Status404NotFound, null, "Domain not found.");
            if (domain.TenantId != tenantId) return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Access denied.");

            try
            {
                var ok = await _domain.ValidateDomainAsync(domainId);
                if (!ok) return BuildEnvelope(StatusCodes.Status400BadRequest, null, "Validation failed or not ready.");

                return BuildEnvelope(StatusCodes.Status200OK, new { validated = true }, "Domain validated.");
            }
            catch (DomainValidationRateLimitException ex)
            {
                Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(ex.RetryAfter.TotalSeconds)).ToString();
                return BuildEnvelope(StatusCodes.Status429TooManyRequests, new
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(ex.RetryAfter.TotalSeconds))
                }, "Validation temporarily rate limited.");
            }
        }

        /// <summary>
        /// Lien pour lister les domaines d'un tenant.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "TenantAdminPolicy")]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> List(Guid tenantId)
        {
            var list = await _domain.ListDomainsAsync(tenantId);
            return BuildEnvelope(StatusCodes.Status200OK, list, "Request processed successfully.");
        }

        private ObjectResult BuildEnvelope(int statusCode, object? data, string message)
        {
            return StatusCode(statusCode, new ApiEnvelopeDto
            {
                statuscode = statusCode,
                data = data,
                message = message
            });
        }
    }
}
