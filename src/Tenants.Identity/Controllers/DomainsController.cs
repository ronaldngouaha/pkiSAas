using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/tenants/{tenantId:guid}/domains")]
    public class DomainsController : ControllerBase
    {
        private readonly IDomainService _domainService;

        public DomainsController(IDomainService domainService)
        {
            _domainService = domainService;
        }

        [HttpPost]
        public async Task<IActionResult> Add(Guid tenantId, [FromBody] AddDomainRequest request)
        {
            await _domainService.AddDomainAsync(tenantId, request.Domain, request.ValidationMethod ?? "dns-txt");
            return Accepted();
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate(Guid tenantId, [FromBody] ValidateDomainRequest request)
        {
            var ok = await _domainService.ValidateDomainAsync(tenantId, request.Domain, request.ChallengeResponse);
            if (!ok)
            {
                return BadRequest(new { message = "Domain validation failed." });
            }

            return Ok(new { validated = true });
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
    }
}
