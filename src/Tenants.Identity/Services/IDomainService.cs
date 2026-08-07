using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs.Domain;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IDomainService
    {
        Task<TenantDomainDto> AddDomainAsync(Guid tenantId, TenantDomainCreateDto dto);
        Task<TenantDomainDto> GetDomainAsync(Guid domainId);
        Task<IEnumerable<TenantDomainDto>> ListDomainsAsync(Guid tenantId);
        Task<string> GenerateDnsChallengeAsync(Guid domainId);
        Task<string> GenerateHttpChallengeAsync(Guid domainId);
        Task<bool> ValidateDomainAsync(Guid domainId);
        Task<Guid?> ResolveTenantByHostAsync(string host);
    }
}
