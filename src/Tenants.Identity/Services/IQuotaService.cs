using System;
using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IQuotaService
    {
        Task<int?> GetMaxCertificatesAsync(Guid tenantId);
        Task SetMaxCertificatesAsync(Guid tenantId, int? maxCertificates);
        Task<bool> CanIssueCertificateAsync(Guid tenantId, int currentlyIssued);
    }
}
