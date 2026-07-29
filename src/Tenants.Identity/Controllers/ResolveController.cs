using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Acme.Pki.Tenants.Identity.Services;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/resolve")]
    public class ResolveController : ControllerBase
    {
        private readonly IDomainService _service;
        public ResolveController(IDomainService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Resolve([FromQuery] string host)
        {
            var tenantId = await _service.ResolveTenantByHostAsync(host);
            if (tenantId == null) return NotFound();
            return Ok(new { tenantId });
        }
    }
}