using System;
using System.Linq;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class DomainService : IDomainService
    {
        private readonly TenantsIdentityDbContext _db;

        public DomainService(TenantsIdentityDbContext db)
        {
            _db = db;
        }

        public async Task AddDomainAsync(Guid tenantId, string domain, string validationMethod = "dns-txt")
        {
            var exists = await _db.TenantDomains.AnyAsync(d => d.TenantId == tenantId && d.Domain == domain);
            if (exists)
            {
                return;
            }

            _db.TenantDomains.Add(new TenantDomain
            {
                TenantId = tenantId,
                Domain = domain,
                ValidationMethod = validationMethod,
                IsValidated = false
            });

            await _db.SaveChangesAsync();
        }

        public async Task<bool> ValidateDomainAsync(Guid tenantId, string domain, string challengeResponse)
        {
            var entity = await _db.TenantDomains.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Domain == domain);
            if (entity == null)
            {
                return false;
            }

            // Placeholder challenge check. Replace with DNS/HTTP challenge worker integration.
            var valid = !string.IsNullOrWhiteSpace(challengeResponse);
            entity.IsValidated = valid;
            await _db.SaveChangesAsync();
            return valid;
        }

        public async Task<Guid?> ResolveTenantByHostAsync(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            var normalized = host.Trim().ToLowerInvariant();
            var domain = await _db.TenantDomains
                .Where(d => d.Domain.ToLower() == normalized)
                .Select(d => new { d.TenantId, d.IsValidated })
                .FirstOrDefaultAsync();

            if (domain == null || !domain.IsValidated)
            {
                return null;
            }

            return domain.TenantId;
        }
    }
}
