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
                .UseInMemoryDatabase(databaseName: $"TenantsTestDb-{System.Guid.NewGuid()}")
                .Options;
            return new TenantsIdentityDbContext(options);
        }

        [Fact]
        public async Task CreateTenant_ShouldReturnTenantDto()
        {
            using var db = CreateDbContext();
            var service = new TenantService(db);
            var dto = new TenantCreateDto
            {
                Name = "TestCo",
                Slug = "testco",
                PrimaryDomain = "test.co",
                PlanTier = "Free",
                Metadata = "{}",
                Domains = new System.Collections.Generic.List<string> { "test.co" }
            };
            var createdBy = System.Guid.Parse("11111111-1111-1111-1111-111111111111");
            var result = await service.CreateAsync(dto, createdBy);
            Assert.NotNull(result);
            Assert.Equal("TestCo", result.Name);
            Assert.Equal(createdBy, result.CreatedBy);
        }
    }
}
