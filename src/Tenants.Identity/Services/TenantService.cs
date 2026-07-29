using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Models;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class TenantService : ITenantService
    {
        private readonly TenantsIdentityDbContext _db;
        public TenantService(TenantsIdentityDbContext db) => _db = db;

        public async Task<TenantDto> CreateAsync(TenantCreateDto dto)
        {
            var tenant = new Tenant
            {
                Name = dto.Name,
                Slug = string.IsNullOrWhiteSpace(dto.Slug) ? Guid.NewGuid().ToString("N") : dto.Slug,
                PrimaryDomain = dto.PrimaryDomain ?? string.Empty,
                PlanTier = dto.PlanTier ?? "Free",
                MaxCertificates = dto.MaxCertificates,
                Metadata = dto.Metadata ?? "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal",
                IsActive = true,
                IsSuspended = false
            };
            _db.Tenants.Add(tenant);
            if (dto.Domains != null)
            {
                foreach (var d in dto.Domains)
                {
                    _db.TenantDomains.Add(new TenantDomain { TenantId = tenant.Id, Domain = d, ValidationMethod = "dns-txt" });
                }
            }
            await _db.SaveChangesAsync();
            return new TenantDto { Id = tenant.Id, Name = tenant.Name, Slug = tenant.Slug, PrimaryDomain = tenant.PrimaryDomain, PlanTier = tenant.PlanTier, MaxCertificates = tenant.MaxCertificates, Metadata = tenant.Metadata, IsActive = tenant.IsActive, IsSuspended = tenant.IsSuspended, CreatedAt = tenant.CreatedAt, Domains = dto.Domains ?? new List<string>() };
        }

        public async Task<TenantDto> GetAsync(Guid tenantId)
        {
            var t = await _db.Tenants.FindAsync(tenantId);
            if (t == null) return null;
            var domains = await _db.TenantDomains.Where(d => d.TenantId == tenantId).Select(d => d.Domain).ToListAsync();
            return new TenantDto { Id = t.Id, Name = t.Name, Slug = t.Slug, PrimaryDomain = t.PrimaryDomain, PlanTier = t.PlanTier, MaxCertificates = t.MaxCertificates, Metadata = t.Metadata, IsActive = t.IsActive, IsSuspended = t.IsSuspended, CreatedAt = t.CreatedAt, Domains = domains };
        }

        public async Task<TenantDto> UpdateAsync(Guid tenantId, TenantCreateDto dto)
        {
            var t = await _db.Tenants.FindAsync(tenantId);
            if (t == null) return null;
            t.Name = dto.Name ?? t.Name;
            t.PrimaryDomain = dto.PrimaryDomain ?? t.PrimaryDomain;
            t.PlanTier = dto.PlanTier ?? t.PlanTier;
            t.MaxCertificates = dto.MaxCertificates ?? t.MaxCertificates;
            t.Metadata = dto.Metadata ?? t.Metadata;
            t.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await GetAsync(tenantId);
        }

        public async Task SuspendAsync(Guid tenantId, string reason)
        {
            var t = await _db.Tenants.FindAsync(tenantId);
            if (t == null) throw new KeyNotFoundException();
            t.IsSuspended = true;
            t.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            // publish audit event (AuditService) - to be wired by DI
        }

        public async Task<IEnumerable<TenantDto>> ListAsync(int page = 1, int pageSize = 50)
        {
            var tenants = await _db.Tenants.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = new List<TenantDto>();
            foreach (var t in tenants)
            {
                var domains = await _db.TenantDomains.Where(d => d.TenantId == t.Id).Select(d => d.Domain).ToListAsync();
                result.Add(new TenantDto { Id = t.Id, Name = t.Name, Slug = t.Slug, PrimaryDomain = t.PrimaryDomain, PlanTier = t.PlanTier, MaxCertificates = t.MaxCertificates, Metadata = t.Metadata, IsActive = t.IsActive, IsSuspended = t.IsSuspended, CreatedAt = t.CreatedAt, Domains = domains });
            }
            return result;
        }
    }
}