using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Controllers;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Models;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class AuthControllerIntrospectTests
    {
        private static TenantsIdentityDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<TenantsIdentityDbContext>()
                .UseInMemoryDatabase(databaseName: $"AuthControllerIntrospect-{Guid.NewGuid()}")
                .Options;
            return new TenantsIdentityDbContext(options);
        }

        [Fact]
        public async Task Introspect_ShouldReturnIdTenantIdAndEmail()
        {
            using var db = CreateDbContext();
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            db.Users.Add(new User
            {
                Id = userId,
                TenantId = tenantId,
                Email = "tenant.admin@test.local",
                NormalizedEmail = "tenant.admin@test.local",
                DisplayName = "Tenant Admin",
                Username = "tenant.admin@test.local",
                Role = TenantRole.TenantAdmin,
                PasswordHash = "hash",
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                IsActive = true,
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1"
            });
            await db.SaveChangesAsync();

            using var rsa = RSA.Create(2048);
            var keyId = Guid.NewGuid().ToString("N");
            var signingKey = new RsaSecurityKey(rsa)
            {
                KeyId = keyId
            };

            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

            var jwt = new JwtSecurityToken(
                issuer: "pki.local.issuer",
                audience: "pki.local.audience",
                claims: new[]
                {
                    new Claim("sub", userId.ToString()),
                    new Claim("tid", tenantId.ToString()),
                    new Claim("roles", "TenantAdmin"),
                    new Claim("metadata", "{\"department\":\"security\"}")
                },
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "pki.local.issuer",
                    ["Jwt:Audience"] = "pki.local.audience"
                })
                .Build();

            var controller = new AuthController(new FakeAuthService(), db, config, new FakeKeyProvider(signingKey));

            var result = await controller.Introspect(new AuthController.IntrospectRequest
            {
                Token = token
            });
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var json = JsonSerializer.Serialize(ok.Value);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

            Assert.Equal(3, payload.Count);
            Assert.Equal(200, payload["statuscode"].GetInt32());
            Assert.Equal("Requete traitee avec succes.", payload["message"].GetString());

            var data = payload["data"];
            Assert.Equal(userId, data.GetProperty("UserId").GetGuid());
            Assert.Equal(tenantId, data.GetProperty("TenantId").GetGuid());
            Assert.Equal("tenant.admin@test.local", data.GetProperty("Email").GetString());
            Assert.Equal("{\"department\":\"security\"}", data.GetProperty("Metadata").GetString());
            Assert.Equal(JsonValueKind.Array, data.GetProperty("Role").ValueKind);
            Assert.Single(data.GetProperty("Role").EnumerateArray());
            Assert.Equal("TenantAdmin", data.GetProperty("Role").EnumerateArray().First().GetString());
            Assert.True(data.GetProperty("RemainingValiditySeconds").GetInt32() > 0);
            Assert.True(data.GetProperty("ExpiresAtUtc").ValueKind == JsonValueKind.String);
        }

        private sealed class FakeAuthService : IAuthService
        {
            public Task<AuthResultDto> LoginAsync(LoginRequestDto dto, string ip) => throw new NotImplementedException();
            public Task<AuthResultDto> RefreshAsync(string refreshToken, string ip) => throw new NotImplementedException();
            public Task RevokeRefreshTokenAsync(string refreshToken, string ip) => throw new NotImplementedException();
            public Task<UserDto> RegisterAsync(Guid? tenantId, RegisterRequestDto dto) => throw new NotImplementedException();
            public Task SeedSuperAdminAsync(RegisterRequestDto dto) => throw new NotImplementedException();
            public Task<bool> ValidatePasswordAsync(string email, string password) => throw new NotImplementedException();
        }

        private sealed class FakeKeyProvider : IKeyProvider
        {
            private readonly RsaSecurityKey _publicKey;

            public FakeKeyProvider(RsaSecurityKey publicKey)
            {
                _publicKey = publicKey;
            }

            public Task<(string KeyId, RSAParameters PrivateKey)> GetActiveRsaKeyAsync()
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPublicJwksAsync()
            {
                if (_publicKey.Rsa == null)
                {
                    throw new InvalidOperationException("Missing RSA key.");
                }

                var p = _publicKey.Rsa.ExportParameters(false);
                var n = Base64UrlEncoder.Encode(p.Modulus!);
                var e = Base64UrlEncoder.Encode(p.Exponent!);

                var jwksJson =
                    "{\"keys\":[{" +
                    "\"kty\":\"RSA\"," +
                    "\"use\":\"sig\"," +
                    "\"alg\":\"RS256\"," +
                    "\"kid\":\"" + _publicKey.KeyId + "\"," +
                    "\"n\":\"" + n + "\"," +
                    "\"e\":\"" + e + "\"" +
                    "}]}";

                return Task.FromResult(jwksJson);
            }
        }
    }
}
