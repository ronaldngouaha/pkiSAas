using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Models;
using Acme.Pki.Tenants.Identity.Security;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class ActiveUserTokenValidatorTests
    {
        private static TenantsIdentityDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<TenantsIdentityDbContext>()
                .UseInMemoryDatabase($"ActiveUserTokenValidatorDb-{Guid.NewGuid()}")
                .Options;

            return new TenantsIdentityDbContext(options);
        }

        [Fact]
        public async Task IsActiveAsync_ShouldReturnFalse_WhenUserIsDisabled()
        {
            await using var db = CreateDbContext();
            var userId = Guid.NewGuid();

            db.Users.Add(new User
            {
                Id = userId,
                TenantId = Guid.NewGuid(),
                Email = "disabled@test.local",
                NormalizedEmail = "disabled@test.local",
                DisplayName = "Disabled",
                Username = "disabled@test.local",
                Role = TenantRole.User,
                PasswordHash = "hash",
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = false
            });
            await db.SaveChangesAsync();

            var validator = new ActiveUserTokenValidator(db);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", userId.ToString())
            }, "test"));

            var isActive = await validator.IsActiveAsync(principal);

            Assert.False(isActive);
        }

        [Fact]
        public async Task IsActiveAsync_ShouldReturnTrue_WhenUserIsEnabled()
        {
            await using var db = CreateDbContext();
            var userId = Guid.NewGuid();

            db.Users.Add(new User
            {
                Id = userId,
                TenantId = Guid.NewGuid(),
                Email = "enabled@test.local",
                NormalizedEmail = "enabled@test.local",
                DisplayName = "Enabled",
                Username = "enabled@test.local",
                Role = TenantRole.User,
                PasswordHash = "hash",
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            });
            await db.SaveChangesAsync();

            var validator = new ActiveUserTokenValidator(db);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", userId.ToString())
            }, "test"));

            var isActive = await validator.IsActiveAsync(principal);

            Assert.True(isActive);
        }
    }
}