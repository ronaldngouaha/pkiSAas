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
        private readonly ITenantService _service;
        public UsersController(ITenantService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Create(Guid tenantId, [FromBody] UserCreateDto dto)
        {
            var user = await _service.CreateUserAsync(tenantId, dto);
            return CreatedAtAction(nameof(List), new { tenantId = tenantId }, user);
        }

        [HttpGet]
        public async Task<IActionResult> List(Guid tenantId)
        {
            var users = await _service.ListUsersAsync(tenantId);
            return Ok(users);
        }
    }
}