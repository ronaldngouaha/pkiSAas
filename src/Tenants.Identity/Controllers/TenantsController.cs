using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Services;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/tenants")]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _service;
        public TenantsController(ITenantService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TenantCreateDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { tenantId = created.Id }, created);
        }

        [HttpGet("{tenantId:guid}")]
        public async Task<IActionResult> Get(Guid tenantId)
        {
            var t = await _service.GetAsync(tenantId);
            if (t == null) return NotFound();
            return Ok(t);
        }

        [HttpPut("{tenantId:guid}")]
        public async Task<IActionResult> Update(Guid tenantId, [FromBody] TenantCreateDto dto)
        {
            var t = await _service.UpdateAsync(tenantId, dto);
            if (t == null) return NotFound();
            return Ok(t);
        }

        [HttpPost("{tenantId:guid}/suspend")]
        public async Task<IActionResult> Suspend(Guid tenantId, [FromBody] SuspendRequest request)
        {
            await _service.SuspendAsync(tenantId, request.Reason);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var list = await _service.ListAsync(page, pageSize);
            return Ok(list);
        }

        public class SuspendRequest { public string Reason { get; set; } }
    }
}