using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Services;

namespace Acme.Pki.Tenants.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/tenants/{tenantId:guid}/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;
        public UsersController(IUserService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Create(Guid tenantId, [FromBody] UserCreateDto dto)
        {
            var user = await _service.CreateAsync(tenantId, dto);
            return CreatedAtAction(nameof(Get), new { tenantId = tenantId, userId = user.Id }, user);
        }

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> Get(Guid tenantId, Guid userId)
        {
            var user = await _service.GetAsync(tenantId, userId);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet]
        public async Task<IActionResult> List(Guid tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var users = await _service.ListAsync(tenantId, page, pageSize);
            return Ok(users);
        }

        [HttpPost("{userId:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid tenantId, Guid userId, [FromBody] ReasonRequest req)
        {
            await _service.DeactivateAsync(tenantId, userId, req.Reason);
            return NoContent();
        }

        [HttpPost("{userId:guid}/reactivate")]
        public async Task<IActionResult> Reactivate(Guid tenantId, Guid userId, [FromBody] ReasonRequest req)
        {
            await _service.ReactivateAsync(tenantId, userId, req.Reason);
            return NoContent();
        }

        public class ReasonRequest { public string Reason { get; set; } }
    }
}