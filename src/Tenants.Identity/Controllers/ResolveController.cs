using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Acme.Pki.Tenants.Identity.Services;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    /// <summary>
    /// Endpoint de resolution host vers tenant.
    /// </summary>
    [ApiController]
    [Route("api/v1/resolve")]
    public class ResolveController : ControllerBase
    {
        private readonly IDomainService _service;
        public ResolveController(IDomainService service) => _service = service;

        /// <summary>
        /// Lien pour trouver le tenant associe a un host.
        /// </summary>
        /// <remarks>
        /// Utilise le domaine/host public et retourne l'identifiant du tenant correspondant.
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> Resolve([FromQuery] string host)
        {
            var tenantId = await _service.ResolveTenantByHostAsync(host);
            if (tenantId == null)
            {
                return BuildEnvelope(StatusCodes.Status404NotFound, null, "Tenant introuvable.");
            }

            return BuildEnvelope(StatusCodes.Status200OK, new { tenantId }, "Requete traitee avec succes.");
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