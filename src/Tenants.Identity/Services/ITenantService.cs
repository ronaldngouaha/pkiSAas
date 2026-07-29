using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface ITenantService
    {
        Task<TenantDto> CreateAsync(TenantCreateDto dto);
        Task<TenantDto> GetAsync(Guid tenantId);
        Task<TenantDto> UpdateAsync(Guid tenantId, TenantCreateDto dto);
        Task SuspendAsync(Guid tenantId, string reason);
        Task<IEnumerable<TenantDto>> ListAsync(int page = 1, int pageSize = 50);
    }
}