using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Controllers;
using Acme.Pki.Tenants.Identity.DTOs.Roles;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class RolesControllerEnvelopeTests
    {
        [Fact]
        public async Task List_ShouldReturnEnvelope_WhenSuperAdmin()
        {
            var controller = CreateController(new FakeRoleCatalogService(), isSuperAdmin: true);

            var result = await controller.List();
            var response = Assert.IsType<ObjectResult>(result);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(StatusCodes.Status200OK, payload["statuscode"].GetInt32());
            Assert.Equal("Request processed successfully.", payload["message"].GetString());
            Assert.Equal(JsonValueKind.Array, payload["data"].ValueKind);
        }

        [Fact]
        public async Task List_ShouldReturnEnvelopeForbidden_WhenTenantClaimMissing()
        {
            var controller = CreateController(new FakeRoleCatalogService());

            var result = await controller.List();
            var response = Assert.IsType<ObjectResult>(result);

            Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(StatusCodes.Status403Forbidden, payload["statuscode"].GetInt32());
            Assert.Equal("Access denied: tenant not found in token.", payload["message"].GetString());
        }

        [Fact]
        public async Task CreateBySuperAdmin_ShouldReturnEnvelopeBadRequest_OnBusinessError()
        {
            var controller = CreateController(new FakeRoleCatalogService(throwOnCreate: true), isSuperAdmin: true);

            var result = await controller.CreateBySuperAdmin(new CreateRoleDefinitionDto
            {
                Name = "RoleX",
                RoleMap = "RoleX",
                Scope = "global",
                Definition = "Def",
                Description = "Desc"
            });

            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(StatusCodes.Status400BadRequest, payload["statuscode"].GetInt32());
            Assert.Equal("Role deja existant.", payload["message"].GetString());
        }

        private static RolesController CreateController(IRoleCatalogService service, bool isSuperAdmin = false)
        {
            var controller = new RolesController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            ClaimsIdentity identity;
            if (isSuperAdmin)
            {
                identity = new ClaimsIdentity(new[] { new Claim("roles", "SuperAdmin") }, "TestAuth");
            }
            else
            {
                identity = new ClaimsIdentity();
            }

            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
            return controller;
        }

        private sealed class FakeRoleCatalogService : IRoleCatalogService
        {
            private readonly bool _throwOnCreate;

            public FakeRoleCatalogService(bool throwOnCreate = false)
            {
                _throwOnCreate = throwOnCreate;
            }

            public Task<IEnumerable<RoleDefinitionDto>> ListAsync(Guid? tenantId, string? scope = null, bool includeInactive = false)
            {
                return Task.FromResult<IEnumerable<RoleDefinitionDto>>(new[]
                {
                    new RoleDefinitionDto
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Name = "TenantAdmin",
                        RoleMap = "TenantAdmin",
                        Scope = tenantId.HasValue ? "tenant" : "global",
                        Definition = "Administration du tenant",
                        Description = "Role test",
                        Attributes = "{}",
                        IsDefault = true,
                        IsSystem = true,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    }
                });
            }

            public Task<RoleDefinitionDto> CreateBySuperAdminAsync(CreateRoleDefinitionDto dto)
            {
                if (_throwOnCreate)
                {
                    throw new InvalidOperationException("Role deja existant.");
                }

                return Task.FromResult(new RoleDefinitionDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = dto.TenantId,
                    Name = dto.Name,
                    RoleMap = dto.RoleMap,
                    Scope = dto.Scope,
                    Definition = dto.Definition,
                    Description = dto.Description,
                    Attributes = dto.Attributes ?? "{}",
                    IsDefault = false,
                    IsSystem = false,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            public Task<RoleDefinitionDto> CreateByTenantAdminAsync(Guid tenantId, CreateRoleDefinitionDto dto)
            {
                return Task.FromResult(new RoleDefinitionDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = dto.Name,
                    RoleMap = dto.RoleMap,
                    Scope = "tenant",
                    Definition = dto.Definition,
                    Description = dto.Description,
                    Attributes = dto.Attributes ?? "{}",
                    IsDefault = false,
                    IsSystem = false,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            public Task SeedDefaultsAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
}
