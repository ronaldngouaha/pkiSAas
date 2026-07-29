using System;
using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IDomainService
    {
        Task AddDomainAsync(Guid tenantId, string domain, string validationMethod = "dns-txt");
        Task<bool> ValidateDomainAsync(Guid tenantId, string domain, string challengeResponse);
        Task<Guid?> ResolveTenantByHostAsync(string host);
    }
}
