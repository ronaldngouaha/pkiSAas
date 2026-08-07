using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Controllers;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.DTOs.SuperAdmin;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class SuperAdminsControllerTests
    {
        [Fact]
        public async Task Create_ShouldAllowBootstrapWithoutBearer_WhenNoActiveSuperAdminExists()
        {
            var service = new FakeSuperAdminService(hasAnyActiveSuperAdmin: false);
            var controller = CreateController(service);

            var result = await controller.Create(new SuperAdminCreateDto
            {
                Email = "bootstrap@pki.local",
                DisplayName = "Bootstrap",
                Password = "AdminPass123$"
            });

            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status201Created, payload["statuscode"].GetInt32());
            Assert.Equal("Request processed successfully.", payload["message"].GetString());
            Assert.Equal("bootstrap@pki.local", payload["data"].GetProperty("Email").GetString());
        }

        [Fact]
        public async Task Create_ShouldForbidWithoutBearer_WhenActiveSuperAdminExists()
        {
            var service = new FakeSuperAdminService(hasAnyActiveSuperAdmin: true);
            var controller = CreateController(service);

            var result = await controller.Create(new SuperAdminCreateDto
            {
                Email = "second@pki.local",
                DisplayName = "Second",
                Password = "AdminPass123$"
            });

            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status403Forbidden, payload["statuscode"].GetInt32());
            Assert.True(payload["data"].ValueKind == JsonValueKind.Null);
            Assert.Equal("Access denied: only a SuperAdmin can create another SuperAdmin.", payload["message"].GetString());
        }

        [Fact]
        public async Task Create_ShouldAllowAuthenticatedSuperAdmin_WhenActiveSuperAdminExists()
        {
            var service = new FakeSuperAdminService(hasAnyActiveSuperAdmin: true);
            var controller = CreateController(service, isSuperAdmin: true);

            var result = await controller.Create(new SuperAdminCreateDto
            {
                Email = "second@pki.local",
                DisplayName = "Second",
                Password = "AdminPass123$"
            });

            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status201Created, payload["statuscode"].GetInt32());
            Assert.Equal("Request processed successfully.", payload["message"].GetString());
            Assert.Equal("second@pki.local", payload["data"].GetProperty("Email").GetString());
        }

        [Fact]
        public async Task List_ShouldReturnEnvelope_WhenCallerIsSuperAdmin()
        {
            var service = new FakeSuperAdminService(hasAnyActiveSuperAdmin: true);
            var controller = CreateController(service, isSuperAdmin: true);

            var result = await controller.List();
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status200OK, payload["statuscode"].GetInt32());
            Assert.Equal("Request processed successfully.", payload["message"].GetString());
            Assert.Equal(JsonValueKind.Array, payload["data"].ValueKind);
        }

        [Fact]
        public async Task Get_ShouldReturnEnvelopeNotFound_WhenSuperAdminDoesNotExist()
        {
            var service = new FakeSuperAdminService(hasAnyActiveSuperAdmin: true);
            var controller = CreateController(service, isSuperAdmin: true);

            var result = await controller.Get(Guid.NewGuid());
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status404NotFound, payload["statuscode"].GetInt32());
            Assert.True(payload["data"].ValueKind == JsonValueKind.Null);
            Assert.Equal("SuperAdmin not found.", payload["message"].GetString());
        }

        [Fact]
        public async Task Deactivate_ShouldReturnEnvelope_WhenCallerIsSuperAdmin()
        {
            var service = new FakeSuperAdminService(hasAnyActiveSuperAdmin: true);
            var controller = CreateController(service, isSuperAdmin: true);

            var result = await controller.Deactivate(Guid.NewGuid(), new SuperAdminStatusRequestDto { Reason = "test" });
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status200OK, payload["statuscode"].GetInt32());
            Assert.Equal("Request processed successfully.", payload["message"].GetString());
        }

        [Fact]
        public async Task ResetTenantUserPassword_ShouldReturnEnvelopeNotFound_WhenUserMissing()
        {
            var service = new FakeSuperAdminService(hasAnyActiveSuperAdmin: true);
            var controller = CreateController(service, isSuperAdmin: true);

            var result = await controller.ResetTenantUserPasswordToDefault(Guid.NewGuid(), Guid.NewGuid());
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(3, payload.Count);
            Assert.Equal(StatusCodes.Status404NotFound, payload["statuscode"].GetInt32());
            Assert.True(payload["data"].ValueKind == JsonValueKind.Null);
            Assert.Equal("User not found for this tenant.", payload["message"].GetString());
        }

        private static SuperAdminsController CreateController(ISuperAdminService service, bool isSuperAdmin = false)
        {
            var controller = new SuperAdminsController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var identity = isSuperAdmin
                ? new ClaimsIdentity(new[] { new Claim("roles", "SuperAdmin") }, "TestAuth")
                : new ClaimsIdentity();

            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
            return controller;
        }

        private sealed class FakeSuperAdminService : ISuperAdminService
        {
            private readonly bool _hasAnyActiveSuperAdmin;

            public FakeSuperAdminService(bool hasAnyActiveSuperAdmin)
            {
                _hasAnyActiveSuperAdmin = hasAnyActiveSuperAdmin;
            }

            public Task<bool> AnyActiveSuperAdminAsync()
            {
                return Task.FromResult(_hasAnyActiveSuperAdmin);
            }

            public Task<UserDto> CreateAsync(SuperAdminCreateDto dto)
            {
                return Task.FromResult(new UserDto
                {
                    Id = Guid.NewGuid(),
                    Email = dto.Email,
                    NormalizedEmail = dto.Email.ToLowerInvariant(),
                    DisplayName = dto.DisplayName,
                    Role = new[] { "SuperAdmin" },
                    IsActive = true,
                    Metadata = "{}"
                });
            }

            public Task<UserDto> CreateTenantAdminAsync(Guid tenantId, TenantAdminCreateDto dto)
            {
                return Task.FromResult(new UserDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Email = dto.Email,
                    NormalizedEmail = dto.Email.ToLowerInvariant(),
                    DisplayName = dto.DisplayName,
                    Role = new[] { "TenantAdmin" },
                    IsActive = true,
                    Metadata = dto.Metadata ?? "{}"
                });
            }

            public Task<UserDto?> GetAsync(Guid id)
            {
                return Task.FromResult<UserDto?>(null);
            }

            public Task<UserDto?> UpdateAsync(Guid id, SuperAdminUpdateDto dto)
            {
                throw new NotSupportedException();
            }

            public Task<IEnumerable<UserDto>> ListAsync(int page = 1, int pageSize = 50, bool includeInactive = false)
            {
                return Task.FromResult<IEnumerable<UserDto>>(new[]
                {
                    new UserDto
                    {
                        Id = Guid.NewGuid(),
                        Email = "superadmin@pki.local",
                        NormalizedEmail = "superadmin@pki.local",
                        DisplayName = "Super Admin",
                        Role = new[] { "SuperAdmin" },
                        IsActive = true,
                        Metadata = "{}"
                    }
                });
            }

            public Task<IEnumerable<UserDto>> ListTenantUsersAsync(Guid tenantId, int page = 1, int pageSize = 50)
            {
                return Task.FromResult<IEnumerable<UserDto>>(Array.Empty<UserDto>());
            }

            public Task DeactivateAsync(Guid id, string reason)
            {
                return Task.CompletedTask;
            }

            public Task ReactivateAsync(Guid id, string reason)
            {
                return Task.CompletedTask;
            }

            public Task ChangePasswordAsync(Guid id, string newPassword)
            {
                return Task.CompletedTask;
            }

            public Task<string> ResetTenantUserPasswordToDefaultAsync(Guid tenantId, Guid userId)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}