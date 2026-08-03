using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Controllers;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class TenantsControllerEnvelopeTests
    {
        [Fact]
        public async Task List_ShouldReturnEnvelope()
        {
            var controller = CreateController(new FakeTenantService());

            var result = await controller.List();
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status200OK, payload["statuscode"].GetInt32());
            Assert.Equal("Requete traitee avec succes.", payload["message"].GetString());
            Assert.Equal(JsonValueKind.Array, payload["data"].ValueKind);
        }

        [Fact]
        public async Task Get_ShouldReturnEnvelopeNotFound_WhenTenantMissing()
        {
            var controller = CreateController(new FakeTenantService());

            var result = await controller.Get(Guid.NewGuid());
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status404NotFound, payload["statuscode"].GetInt32());
            Assert.True(payload["data"].ValueKind == JsonValueKind.Null);
            Assert.Equal("Tenant introuvable.", payload["message"].GetString());
        }

        [Fact]
        public async Task Create_ShouldReturnEnvelopeForbidden_WhenSubClaimMissing()
        {
            var controller = CreateController(new FakeTenantService(), includeSubClaim: false);

            var result = await controller.Create(new TenantCreateDto
            {
                Name = "Demo",
                Slug = "demo",
                PrimaryDomain = "demo.test.local",
                PlanTier = "Standard",
                MaxCertificates = 100,
                Metadata = "{}",
                Domains = new List<string> { "demo.test.local" }
            });

            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status403Forbidden, payload["statuscode"].GetInt32());
            Assert.True(payload["data"].ValueKind == JsonValueKind.Null);
            Assert.Equal("Acces refuse: role SuperAdmin requis.", payload["message"].GetString());
        }

        private static TenantsController CreateController(ITenantService service, bool includeSubClaim = true)
        {
            var controller = new TenantsController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var identity = includeSubClaim
                ? new ClaimsIdentity(new[] { new Claim("sub", Guid.NewGuid().ToString()) }, "TestAuth")
                : new ClaimsIdentity();

            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
            return controller;
        }

        private sealed class FakeTenantService : ITenantService
        {
            public Task<TenantDto> CreateAsync(TenantCreateDto dto, Guid createdBy)
            {
                return Task.FromResult(new TenantDto
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Slug = dto.Slug,
                    PrimaryDomain = dto.PrimaryDomain,
                    CreatedBy = createdBy,
                    PlanTier = dto.PlanTier,
                    MaxCertificates = dto.MaxCertificates,
                    Metadata = dto.Metadata,
                    IsActive = true,
                    IsSuspended = false,
                    CreatedAt = DateTime.UtcNow,
                    Domains = dto.Domains
                });
            }

            public Task<TenantDto?> GetAsync(Guid tenantId)
            {
                return Task.FromResult<TenantDto?>(null);
            }

            public Task<IEnumerable<TenantDto>> ListAsync(int page = 1, int pageSize = 50)
            {
                var list = new[]
                {
                    new TenantDto
                    {
                        Id = Guid.NewGuid(),
                        Name = "Demo Tenant",
                        Slug = "demo-tenant",
                        PrimaryDomain = "demo.test.local",
                        CreatedBy = Guid.NewGuid(),
                        PlanTier = "Standard",
                        MaxCertificates = 100,
                        Metadata = "{}",
                        IsActive = true,
                        IsSuspended = false,
                        CreatedAt = DateTime.UtcNow,
                        Domains = new List<string> { "demo.test.local" }
                    }
                };

                return Task.FromResult<IEnumerable<TenantDto>>(list);
            }

            public Task<TenantDto?> SuspendAsync(Guid tenantId, string reason)
            {
                throw new KeyNotFoundException();
            }

            public Task<TenantDto?> UpdateAsync(Guid tenantId, TenantCreateDto dto)
            {
                return Task.FromResult<TenantDto?>(null);
            }
        }
    }
}
