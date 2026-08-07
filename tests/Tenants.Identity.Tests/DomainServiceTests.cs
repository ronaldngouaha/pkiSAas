using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using DnsClient;
using System.Net.Http;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Services;
using Acme.Pki.Tenants.Identity.Models;
using Acme.Pki.Tenants.Identity.DTOs;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class DomainServiceTests
    {
        private TenantsIdentityDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<TenantsIdentityDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TenantsIdentityDbContext(options);
        }

        [Fact]
        public async Task AddDomain_GeneratesChallenge()
        {
            using var db = CreateDbContext();
            var tenant = new Tenant { Name = "T", PrimaryDomain = "t.local" };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var dns = new LookupClient();
            var http = new HttpClient();
            var svc = new DomainService(db, new NullLogger<DomainService>(), dns, http);

            var dto = new Domain.TenantDomainCreateDto { Domain = "example.local", ValidationMethod = "dns" };
            var created = await svc.AddDomainAsync(tenant.Id, dto);

            Assert.Equal("example.local", created.Domain);
            Assert.False(created.IsValidated);
            Assert.False(string.IsNullOrEmpty(created.Challenge));
        }

        [Fact]
        public async Task ResolveTenantByHost_ReturnsTenant()
        {
            using var db = CreateDbContext();
            var tenant = new Tenant { Name = "Acme", PrimaryDomain = "acme.example" };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var dns = new LookupClient();
            var http = new HttpClient();
            var svc = new DomainService(db, new NullLogger<DomainService>(), dns, http);

            var resolved = await svc.ResolveTenantByHostAsync("app.acme.example");
            Assert.Equal(tenant.Id, resolved);
        }
    }
}
