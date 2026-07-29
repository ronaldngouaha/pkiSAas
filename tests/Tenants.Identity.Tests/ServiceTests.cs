using System;
using System.Linq;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class ServiceTests
    {
        private static TenantsIdentityDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<TenantsIdentityDbContext>()
                .UseInMemoryDatabase($"ServiceTests-{Guid.NewGuid()}")
                .Options;
            return new TenantsIdentityDbContext(options);
        }

        [Fact]
        public async Task TenantService_CreateAndGet_ShouldWork()
        {
            using var db = CreateDbContext();
            var service = new TenantService(db);

            var created = await service.CreateAsync(new TenantCreateDto
            {
                Name = "Acme",
                Slug = "acme",
                PrimaryDomain = "acme.test",
                PlanTier = "Standard",
                MaxCertificates = 100,
                Metadata = "{}",
                Domains = new() { "acme.test" }
            });

            var fetched = await service.GetAsync(created.Id);

            Assert.NotNull(fetched);
            Assert.Equal("Acme", fetched.Name);
            Assert.Contains("acme.test", fetched.Domains);
        }

        [Fact]
        public async Task UserService_CreateAndList_ShouldWork()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "Tenant",
                Slug = "tenant",
                PrimaryDomain = "tenant.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var service = new UserService(db);
            await service.CreateAsync(tenant.Id, new UserCreateDto
            {
                Email = "user@tenant.test",
                DisplayName = "User",
                Password = "P@ssw0rd123!",
                Role = "User",
                MfaEnabled = false,
                Metadata = "{}"
            });

            var users = (await service.ListAsync(tenant.Id)).ToList();
            Assert.Single(users);
            Assert.Equal("user@tenant.test", users[0].Email);
        }

        [Fact]
        public async Task DomainService_ResolveValidatedHost_ShouldReturnTenantId()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "DomainTenant",
                Slug = "domaintenant",
                PrimaryDomain = "domain.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var service = new DomainService(db);
            await service.AddDomainAsync(tenant.Id, "domain.test");
            await service.ValidateDomainAsync(tenant.Id, "domain.test", "ok");

            var resolved = await service.ResolveTenantByHostAsync("domain.test");
            Assert.Equal(tenant.Id, resolved);
        }
    }
}
