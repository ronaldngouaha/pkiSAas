using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Services;
using Acme.Pki.Tenants.Identity.DTOs;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class TenantsControllerTests
    {
        private TenantsIdentityDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<TenantsIdentityDbContext>()
                .UseInMemoryDatabase(databaseName: "TenantsTestDb")
                .Options;
            return new TenantsIdentityDbContext(options);
        }

        [Fact]
        public async Task CreateTenant_ShouldReturnTenantDto()
        {
            using var db = CreateDbContext();
            var service = new TenantService(db);
            var dto = new TenantCreateDto { Name = "TestCo", Domains = new System.Collections.Generic.List<string> { "test.co" } };
            var result = await service.CreateTenantAsync(dto);
            Assert.NotNull(result);
            Assert.Equal("TestCo", result.Name);
        }
    }
}
