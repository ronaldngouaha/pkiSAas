using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Services;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    /// <summary>
    /// Endpoints de gestion des tenants.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "SuperAdminOnly")]
    [Route("api/v1/tenants")]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _service;
        public TenantsController(ITenantService service) => _service = service;

        /// <summary>
        /// Lien pour creer un tenant.
        /// </summary>
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TenantCreateDto dto)
        {
            var createdBy = GetCurrentSuperAdminId();
            if (createdBy == null)
            {
                return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Acces refuse: role SuperAdmin requis.");
            }

            var created = await _service.CreateAsync(dto, createdBy.Value);
            return BuildEnvelope(StatusCodes.Status201Created, created, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour recuperer les details d'un tenant.
        /// </summary>
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpGet("{tenantId:guid}")]
        public async Task<IActionResult> Get(Guid tenantId)
        {
            var t = await _service.GetAsync(tenantId);
            if (t == null)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Tenant introuvable.");
            }

            return BuildEnvelope(StatusCodes.Status200OK, t, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour modifier un tenant existant.
        /// </summary>
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPut("{tenantId:guid}")]
        public async Task<IActionResult> Update(Guid tenantId, [FromBody] TenantCreateDto dto)
        {
            var t = await _service.UpdateAsync(tenantId, dto);
            if (t == null)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Tenant introuvable.");
            }

            return BuildEnvelope(StatusCodes.Status200OK, t, "Requete traitee avec succes.");
        }

        /// <summary>
        /// Lien pour suspendre un tenant.
        /// </summary>
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost("{tenantId:guid}/suspend")]
        public async Task<IActionResult> Suspend(Guid tenantId, [FromBody] SuspendRequest request)
        {
            try
            {
                var suspended = await _service.SuspendAsync(tenantId, request.Reason);
                return BuildEnvelope(StatusCodes.Status200OK, suspended, "Requete traitee avec succes.");
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Tenant introuvable.");
            }
        }

        /// <summary>
        /// Lien pour lister les tenants avec pagination.
        /// </summary>
        [ProducesResponseType(typeof(TenantListEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(TenantSingleEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var list = await _service.ListAsync(page, pageSize);
            return BuildEnvelope(StatusCodes.Status200OK, list, "Requete traitee avec succes.");
        }

        public class SuspendRequest { public string Reason { get; set; } }

        private Guid? GetCurrentSuperAdminId()
        {
            var sub = User.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : null;
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