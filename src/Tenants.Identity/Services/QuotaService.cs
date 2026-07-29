using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class QuotaService : IQuotaService
    {
        private readonly TenantsIdentityDbContext _db;

        public QuotaService(TenantsIdentityDbContext db)
        {
            _db = db;
        }

        public async Task<int?> GetMaxCertificatesAsync(Guid tenantId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            return tenant?.MaxCertificates;
        }

        public async Task SetMaxCertificatesAsync(Guid tenantId, int? maxCertificates)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null)
            {
                return;
            }

            tenant.MaxCertificates = maxCertificates;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task<bool> CanIssueCertificateAsync(Guid tenantId, int currentlyIssued)
        {
            var max = await GetMaxCertificatesAsync(tenantId);
            if (!max.HasValue)
            {
                return true;
            }

            return currentlyIssued < max.Value;
        }
    }
}
