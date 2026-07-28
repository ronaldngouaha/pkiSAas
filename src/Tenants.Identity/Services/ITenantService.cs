using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface ITenantService
    {
        Task<TenantDto> CreateTenantAsync(TenantCreateDto dto);
        Task<TenantDto> GetTenantAsync(Guid tenantId);
        Task<IEnumerable<TenantDto>> ListTenantsAsync(int page = 1, int pageSize = 50);
        Task<UserDto> CreateUserAsync(Guid tenantId, UserCreateDto dto);
        Task<IEnumerable<UserDto>> ListUsersAsync(Guid tenantId);
        Task<Guid?> ResolveTenantByHostAsync(string host);
    }
}