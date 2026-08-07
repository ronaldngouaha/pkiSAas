using System;
using System.Linq;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.DTOs.Roles;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/roles")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleCatalogService _service;

        public RolesController(IRoleCatalogService service)
        {
            _service = service;
        }

        /// <summary>
        /// Liste les roles disponibles (roles par defaut + roles tenant selon le contexte).
        /// </summary>
        [Authorize]
        [ProducesResponseType(typeof(RoleListEnvelopeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? scope = null, [FromQuery] bool includeInactive = false)
        {
            var isSuperAdmin = User.HasClaim("roles", "SuperAdmin");
            Guid? tenantId = null;

            if (!isSuperAdmin)
            {
                var tidClaim = User.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;
                if (!Guid.TryParse(tidClaim, out var parsedTenantId))
                {
                    return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Access denied: tenant not found in token.");
                }

                tenantId = parsedTenantId;
            }

            var roles = await _service.ListAsync(tenantId, scope, includeInactive && isSuperAdmin);
            return BuildEnvelope(StatusCodes.Status200OK, roles, "Request processed successfully.");
        }

        /// <summary>
        /// Permet au SuperAdmin d'ajouter un role global ou tenant.
        /// </summary>
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(RoleSingleEnvelopeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> CreateBySuperAdmin([FromBody] CreateRoleDefinitionDto dto)
        {
            try
            {
                var created = await _service.CreateBySuperAdminAsync(dto);
                return BuildEnvelope(StatusCodes.Status201Created, created, "Request processed successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, ex.Message);
            }
        }

        /// <summary>
        /// Permet a un TenantAdmin d'ajouter un role de scope tenant uniquement.
        /// </summary>
        [Authorize(Policy = "TenantAdminPolicy")]
        [ProducesResponseType(typeof(RoleSingleEnvelopeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiEnvelopeDto), StatusCodes.Status500InternalServerError)]
        [HttpPost("tenant")]
        public async Task<IActionResult> CreateByTenantAdmin([FromBody] CreateRoleDefinitionDto dto)
        {
            var tidClaim = User.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;
            if (!Guid.TryParse(tidClaim, out var tenantId))
            {
                return BuildEnvelope(StatusCodes.Status403Forbidden, null, "Access denied: tenant not found in token.");
            }

            try
            {
                var created = await _service.CreateByTenantAdminAsync(tenantId, dto);
                return BuildEnvelope(StatusCodes.Status201Created, created, "Request processed successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return BuildEnvelope(StatusCodes.Status400BadRequest, null, ex.Message);
            }
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
