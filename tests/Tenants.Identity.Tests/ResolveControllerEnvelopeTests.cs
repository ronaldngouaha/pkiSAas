using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Controllers;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class ResolveControllerEnvelopeTests
    {
        [Fact]
        public async Task Resolve_ShouldReturnEnvelopeNotFound_WhenHostUnknown()
        {
            var controller = new ResolveController(new FakeDomainService());

            var result = await controller.Resolve("unknown.test.local");
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(StatusCodes.Status404NotFound, payload["statuscode"].GetInt32());
            Assert.Equal("Tenant introuvable.", payload["message"].GetString());
        }

        private sealed class FakeDomainService : IDomainService
        {
            public Task AddDomainAsync(Guid tenantId, string domain, string validationMethod = "dns-txt")
                => Task.CompletedTask;

            public Task<bool> ValidateDomainAsync(Guid tenantId, string domain, string challengeResponse)
                => Task.FromResult(true);

            public Task<Guid?> ResolveTenantByHostAsync(string host)
                => Task.FromResult<Guid?>(null);
        }
    }
}
