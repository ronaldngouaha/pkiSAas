using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Controllers;
using Acme.Pki.Tenants.Identity.DTOs.Mfa;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class MfaControllerEnvelopeTests
    {
        [Fact]
        public async Task Status_ShouldReturnEnvelope()
        {
            var controller = new MfaController(new FakeMfaService());

            var result = await controller.Status(Guid.NewGuid());
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(StatusCodes.Status200OK, payload["statuscode"].GetInt32());
            Assert.Equal("Request processed successfully.", payload["message"].GetString());
            Assert.True(payload["data"].GetProperty("mfaEnabled").GetBoolean());
        }

        [Fact]
        public async Task VerifyTotp_ShouldReturnEnvelopeBadRequest_WhenInvalidCode()
        {
            var controller = new MfaController(new FakeMfaService());

            var result = await controller.VerifyTotp(Guid.NewGuid(), new MfaVerifyDto { Code = "000000" });
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);

            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(response.Value))!;
            Assert.Equal(StatusCodes.Status400BadRequest, payload["statuscode"].GetInt32());
            Assert.Equal("Invalid code", payload["message"].GetString());
        }

        private sealed class FakeMfaService : IMfaService
        {
            public Task<MfaSetupDto> BeginTotpSetupAsync(Guid userId)
                => Task.FromResult(new MfaSetupDto { ManualEntryKey = "KEY", QrCodePng = new byte[] { 1, 2, 3 } });

            public Task<bool> VerifyTotpAsync(Guid userId, string code)
                => Task.FromResult(false);

            public Task<string[]> GenerateRecoveryCodesAsync(Guid userId, int count = 10)
                => Task.FromResult(new[] { "ABCDEF" });

            public Task<bool> ConsumeRecoveryCodeAsync(Guid userId, string code)
                => Task.FromResult(true);

            public Task DisableTotpAsync(Guid userId)
                => Task.CompletedTask;

            public Task<bool> IsMfaEnabledAsync(Guid userId)
                => Task.FromResult(true);
        }
    }
}
