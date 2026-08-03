using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs.Roles;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IRoleCatalogService
    {
        Task<IEnumerable<RoleDefinitionDto>> ListAsync(Guid? tenantId, string? scope = null, bool includeInactive = false);
        Task<RoleDefinitionDto> CreateBySuperAdminAsync(CreateRoleDefinitionDto dto);
        Task<RoleDefinitionDto> CreateByTenantAdminAsync(Guid tenantId, CreateRoleDefinitionDto dto);
        Task SeedDefaultsAsync();
    }
}
