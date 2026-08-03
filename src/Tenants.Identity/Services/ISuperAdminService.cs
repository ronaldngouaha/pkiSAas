using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs.SuperAdmin;
using Acme.Pki.Tenants.Identity.DTOs;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface ISuperAdminService
    {
        Task<bool> AnyActiveSuperAdminAsync();
        Task<UserDto> CreateAsync(SuperAdminCreateDto dto);
        Task<UserDto> CreateTenantAdminAsync(Guid tenantId, TenantAdminCreateDto dto);
        Task<UserDto?> GetAsync(Guid id);
        Task<UserDto?> UpdateAsync(Guid id, SuperAdminUpdateDto dto);
        Task<IEnumerable<UserDto>> ListAsync(int page = 1, int pageSize = 50, bool includeInactive = false);
        Task<IEnumerable<UserDto>> ListTenantUsersAsync(Guid tenantId, int page = 1, int pageSize = 50);
        Task<string> ResetTenantUserPasswordToDefaultAsync(Guid tenantId, Guid userId);
        Task DeactivateAsync(Guid id, string reason);
        Task ReactivateAsync(Guid id, string reason);
        Task ChangePasswordAsync(Guid id, string newPassword);
    }
}
