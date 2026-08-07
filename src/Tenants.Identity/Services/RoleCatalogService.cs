using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs.Roles;
using Acme.Pki.Tenants.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class RoleCatalogService : IRoleCatalogService
    {
        private const string ScopeGlobal = "Global";
        private const string ScopeTenant = "Tenant";

        private readonly TenantsIdentityDbContext _db;

        public RoleCatalogService(TenantsIdentityDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<RoleDefinitionDto>> ListAsync(Guid? tenantId, string? scope = null, bool includeInactive = false)
        {
            await EnsureStorageAsync();

            var normalizedScope = NormalizeScope(scope, allowEmpty: true);
            var query = _db.RoleCatalogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedScope))
            {
                query = query.Where(r => r.Scope == normalizedScope);
            }

            if (!includeInactive)
            {
                query = query.Where(r => r.IsActive);
            }

            if (tenantId.HasValue)
            {
                query = query.Where(r => r.TenantId == null || r.TenantId == tenantId.Value);
            }

            var roles = await query
                .OrderByDescending(r => r.IsDefault)
                .ThenBy(r => r.Scope)
                .ThenBy(r => r.Name)
                .ToListAsync();

            return roles.Select(Map).ToList();
        }

        public async Task<RoleDefinitionDto> CreateBySuperAdminAsync(CreateRoleDefinitionDto dto)
        {
            await EnsureStorageAsync();

            var entity = await BuildEntityAsync(dto, dto.TenantId, canCreateGlobal: true, canCreateTenant: true);
            _db.RoleCatalogs.Add(entity);
            await _db.SaveChangesAsync();
            return Map(entity);
        }

        public async Task<RoleDefinitionDto> CreateByTenantAdminAsync(Guid tenantId, CreateRoleDefinitionDto dto)
        {
            await EnsureStorageAsync();

            var entity = await BuildEntityAsync(dto, tenantId, canCreateGlobal: false, canCreateTenant: true);
            _db.RoleCatalogs.Add(entity);
            await _db.SaveChangesAsync();
            return Map(entity);
        }

        public async Task SeedDefaultsAsync()
        {
            await EnsureStorageAsync();

            foreach (var role in BuildDefaultRoles())
            {
                var normalized = NormalizeName(role.Name);
                var exists = await _db.RoleCatalogs
                    .AnyAsync(r => r.TenantId == null && r.NormalizedName == normalized && r.IsDefault);

                if (exists)
                {
                    continue;
                }

                role.NormalizedName = normalized;
                _db.RoleCatalogs.Add(role);
            }

            await _db.SaveChangesAsync();
        }

        private async Task<RoleCatalog> BuildEntityAsync(
            CreateRoleDefinitionDto dto,
            Guid? enforcedTenantId,
            bool canCreateGlobal,
            bool canCreateTenant)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new InvalidOperationException("Role name is required.");
            }

            var normalizedScope = NormalizeScope(dto.Scope, allowEmpty: false);
            if (normalizedScope == ScopeGlobal && !canCreateGlobal)
            {
                throw new InvalidOperationException("Tenant admin can only create roles with Tenant scope.");
            }

            if (normalizedScope == ScopeTenant && !canCreateTenant)
            {
                throw new InvalidOperationException("Role scope is not allowed for this caller.");
            }

            var targetTenantId = normalizedScope == ScopeTenant
                ? enforcedTenantId
                : null;

            if (normalizedScope == ScopeGlobal && enforcedTenantId.HasValue && !canCreateGlobal)
            {
                throw new InvalidOperationException("Global roles are not allowed for this caller.");
            }

            var normalizedName = NormalizeName(dto.Name);

            var exists = await _db.RoleCatalogs.AnyAsync(r =>
                r.TenantId == targetTenantId &&
                r.NormalizedName == normalizedName &&
                r.Scope == normalizedScope);

            if (exists)
            {
                throw new InvalidOperationException("Role already exists for this scope.");
            }

            return new RoleCatalog
            {
                TenantId = targetTenantId,
                Name = dto.Name.Trim(),
                NormalizedName = normalizedName,
                RoleMap = string.IsNullOrWhiteSpace(dto.RoleMap) ? dto.Name.Trim() : dto.RoleMap.Trim(),
                Scope = normalizedScope,
                Definition = dto.Definition?.Trim() ?? string.Empty,
                Description = dto.Description?.Trim() ?? string.Empty,
                Attributes = string.IsNullOrWhiteSpace(dto.Attributes) ? "{}" : dto.Attributes,
                IsDefault = false,
                IsSystem = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static IEnumerable<RoleCatalog> BuildDefaultRoles()
        {
            return new List<RoleCatalog>
            {
                DefaultRole("SuperAdmin", ScopeGlobal, "TenantRole.SuperAdmin", "Global administration", "Manages the entire platform, tenants, and security."),
                DefaultRole("TenantOwner", ScopeTenant, "TenantRole.TenantOwner", "Tenant owner", "Primary owner of a tenant and its governance."),
                DefaultRole("TenantAdmin", ScopeTenant, "TenantRole.TenantAdmin", "Tenant administrator", "Manages tenant users, roles, and configuration."),
                DefaultRole("SecurityAdmin", ScopeTenant, "TenantRole.SecurityAdmin", "Security administrator", "Oversees MFA, security policies, and incidents."),
                DefaultRole("AppAdmin", ScopeTenant, "TenantRole.AppAdmin", "Application administrator", "Manages application functional configuration for the tenant."),
                DefaultRole("UserManager", ScopeTenant, "TenantRole.UserManager", "User manager", "Creates and maintains tenant user accounts."),
                DefaultRole("SupportAgent", ScopeTenant, "TenantRole.SupportAgent", "Support agent", "Assists users and handles support requests."),
                DefaultRole("EndUser", ScopeTenant, "TenantRole.EndUser", "End user", "Uses authorized business features."),
                DefaultRole("User", ScopeTenant, "TenantRole.User", "Standard user", "Standard user role with limited access."),
                DefaultRole("Viewer", ScopeTenant, "TenantRole.Viewer", "Read-only", "Read-only access to tenant data."),
                DefaultRole("ReadOnlyAdmin", ScopeTenant, "TenantRole.ReadOnlyAdmin", "Read-only administrator", "Full read-only access to resources."),
                DefaultRole("ServiceAccount", ScopeTenant, "TenantRole.ServiceAccount", "Service account", "Technical account used for integrations and automation.")
            };
        }

        private static RoleCatalog DefaultRole(string name, string scope, string roleMap, string definition, string description)
        {
            return new RoleCatalog
            {
                TenantId = null,
                Name = name,
                NormalizedName = NormalizeName(name),
                Scope = scope,
                RoleMap = roleMap,
                Definition = definition,
                Description = description,
                Attributes = "{\"seeded\":true}",
                IsDefault = true,
                IsSystem = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static string NormalizeName(string input)
        {
            return input.Trim().ToUpperInvariant();
        }

        private static string? NormalizeScope(string? scope, bool allowEmpty)
        {
            if (string.IsNullOrWhiteSpace(scope))
            {
                if (allowEmpty)
                {
                    return null;
                }

                throw new InvalidOperationException("Scope is required. Allowed values: Global, Tenant.");
            }

            if (string.Equals(scope, ScopeGlobal, StringComparison.OrdinalIgnoreCase))
            {
                return ScopeGlobal;
            }

            if (string.Equals(scope, ScopeTenant, StringComparison.OrdinalIgnoreCase))
            {
                return ScopeTenant;
            }

            throw new InvalidOperationException("Invalid scope. Allowed values: Global, Tenant.");
        }

        private async Task EnsureStorageAsync()
        {
            if (!_db.Database.IsSqlServer())
            {
                return;
            }

            const string sql = @"
IF OBJECT_ID(N'dbo.RoleCatalogs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RoleCatalogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [TenantId] UNIQUEIDENTIFIER NULL,
        [Name] NVARCHAR(120) NOT NULL,
        [NormalizedName] NVARCHAR(120) NOT NULL,
        [RoleMap] NVARCHAR(200) NOT NULL,
        [Scope] NVARCHAR(32) NOT NULL,
        [Definition] NVARCHAR(300) NOT NULL,
        [Description] NVARCHAR(1000) NOT NULL,
        [Attributes] NVARCHAR(MAX) NOT NULL,
        [IsDefault] BIT NOT NULL,
        [IsSystem] BIT NOT NULL,
        [IsActive] BIT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_RoleCatalogs] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_RoleCatalogs_TenantId_NormalizedName] ON [dbo].[RoleCatalogs]([TenantId], [NormalizedName]);
    CREATE INDEX [IX_RoleCatalogs_Scope_IsDefault] ON [dbo].[RoleCatalogs]([Scope], [IsDefault]);
END";

            await _db.Database.ExecuteSqlRawAsync(sql);
        }

        private static RoleDefinitionDto Map(RoleCatalog role)
        {
            return new RoleDefinitionDto
            {
                Id = role.Id,
                TenantId = role.TenantId,
                Name = role.Name,
                RoleMap = role.RoleMap,
                Scope = role.Scope,
                Definition = role.Definition,
                Description = role.Description,
                Attributes = role.Attributes,
                IsDefault = role.IsDefault,
                IsSystem = role.IsSystem,
                IsActive = role.IsActive,
                CreatedAtUtc = role.CreatedAt,
                UpdatedAtUtc = role.UpdatedAt
            };
        }
    }
}
