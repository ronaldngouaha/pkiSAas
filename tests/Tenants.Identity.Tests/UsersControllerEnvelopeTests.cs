using System;
using System.Collections.Generic;
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
    public class UsersControllerEnvelopeTests
    {
        [Fact]
        public async Task List_ShouldReturnEnvelope()
        {
            var controller = new UsersController(new FakeUserService());

            var result = await controller.List(Guid.NewGuid());
            var response = Assert.IsType<ObjectResult>(result);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(StatusCodes.Status200OK, payload["statuscode"].GetInt32());
            Assert.Equal("Requete traitee avec succes.", payload["message"].GetString());
            Assert.Equal(JsonValueKind.Array, payload["data"].ValueKind);
        }

        [Fact]
        public async Task Get_ShouldReturnEnvelopeNotFound_WhenUserMissing()
        {
            var controller = new UsersController(new FakeUserService());

            var result = await controller.Get(Guid.NewGuid(), Guid.NewGuid());
            var response = Assert.IsType<ObjectResult>(result);

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(StatusCodes.Status404NotFound, payload["statuscode"].GetInt32());
            Assert.Equal("Utilisateur introuvable.", payload["message"].GetString());
            Assert.Equal(JsonValueKind.Null, payload["data"].ValueKind);
        }

        [Fact]
        public async Task ChangePassword_ShouldReturnEnvelopeBadRequest_WhenPasswordInvalid()
        {
            var controller = new UsersController(new FakeUserService());

            var result = await controller.ChangePassword(Guid.NewGuid(), Guid.NewGuid(), new ChangePasswordRequestDto
            {
                NewPassword = string.Empty
            });

            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(StatusCodes.Status400BadRequest, payload["statuscode"].GetInt32());
            Assert.Equal("New password is required.", payload["message"].GetString());
        }

        private sealed class FakeUserService : IUserService
        {
            public Task<UserDto> CreateAsync(Guid tenantId, UserCreateDto dto)
            {
                return Task.FromResult(new UserDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Email = dto.Email,
                    NormalizedEmail = dto.Email,
                    DisplayName = dto.DisplayName,
                    Role = new[] { "TenantAdmin" },
                    IsActive = true,
                    Metadata = "{}"
                });
            }

            public Task<UserDto> GetAsync(Guid tenantId, Guid userId)
            {
                return Task.FromResult<UserDto>(null!);
            }

            public Task<UserDto> UpdateAsync(Guid tenantId, Guid userId, UserUpdateDto dto)
            {
                return Task.FromResult<UserDto>(null!);
            }

            public Task<UserDto> AddRoleAsync(Guid tenantId, Guid userId, string role)
            {
                return Task.FromResult(new UserDto
                {
                    Id = userId,
                    TenantId = tenantId,
                    Email = "tenant.user@test.local",
                    NormalizedEmail = "tenant.user@test.local",
                    DisplayName = "Tenant User",
                    Role = new[] { role },
                    IsActive = true,
                    Metadata = "{}"
                });
            }

            public Task<IEnumerable<UserDto>> ListAsync(Guid tenantId, int page = 1, int pageSize = 50)
            {
                return Task.FromResult<IEnumerable<UserDto>>(new[]
                {
                    new UserDto
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Email = "tenant.user@test.local",
                        NormalizedEmail = "tenant.user@test.local",
                        DisplayName = "Tenant User",
                        Role = new[] { "TenantAdmin" },
                        IsActive = true,
                        Metadata = "{}"
                    }
                });
            }

            public Task DeactivateAsync(Guid tenantId, Guid userId, string reason)
            {
                return Task.CompletedTask;
            }

            public Task ReactivateAsync(Guid tenantId, Guid userId, string reason)
            {
                return Task.CompletedTask;
            }

            public Task ChangePasswordAsync(Guid tenantId, Guid userId, string newPassword)
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    throw new InvalidOperationException("New password is required.");
                }

                return Task.CompletedTask;
            }
        }
    }
}
