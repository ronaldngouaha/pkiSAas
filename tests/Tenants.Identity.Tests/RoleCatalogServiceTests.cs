using System;
using System.Linq;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs.Roles;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class RoleCatalogServiceTests
    {
        private static TenantsIdentityDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<TenantsIdentityDbContext>()
                .UseInMemoryDatabase($"RoleCatalogServiceTests-{Guid.NewGuid()}")
                .Options;
            return new TenantsIdentityDbContext(options);
        }

        [Fact]
        public async Task SeedDefaultsAsync_ShouldCreateDefaultRoles()
        {
            using var db = CreateDbContext();
            var service = new RoleCatalogService(db);

            await service.SeedDefaultsAsync();
            var roles = (await service.ListAsync(null)).ToList();

            Assert.Contains(roles, r => r.Name == "SuperAdmin" && r.Scope == "Global" && r.IsDefault);
            Assert.Contains(roles, r => r.Name == "TenantAdmin" && r.Scope == "Tenant" && r.IsDefault);
            Assert.True(roles.Count >= 12);
        }

        [Fact]
        public async Task CreateByTenantAdminAsync_ShouldRejectGlobalScope()
        {
            using var db = CreateDbContext();
            var service = new RoleCatalogService(db);
            var tenantId = Guid.NewGuid();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateByTenantAdminAsync(
                tenantId,
                new CreateRoleDefinitionDto
                {
                    Name = "GlobalAuditor",
                    RoleMap = "TenantRole.GlobalAuditor",
                    Scope = "Global",
                    Definition = "Audit global",
                    Description = "Role global interdit pour tenant admin"
                }));

            Assert.Contains("Tenant admin", ex.Message);
        }

        [Fact]
        public async Task CreateByTenantAdminAsync_ShouldCreateTenantScopedRole()
        {
            using var db = CreateDbContext();
            var service = new RoleCatalogService(db);
            var tenantId = Guid.NewGuid();

            var created = await service.CreateByTenantAdminAsync(
                tenantId,
                new CreateRoleDefinitionDto
                {
                    Name = "BillingOperator",
                    RoleMap = "TenantRole.BillingOperator",
                    Scope = "Tenant",
                    Definition = "Gestion facturation",
                    Description = "Peut gerer les operations de facturation"
                });

            Assert.Equal("Tenant", created.Scope);
            Assert.Equal(tenantId, created.TenantId);
            Assert.False(created.IsDefault);
            Assert.False(created.IsSystem);
        }
    }
}
