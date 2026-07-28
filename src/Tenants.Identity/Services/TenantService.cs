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

        public TenantService(TenantsIdentityDbContext db)
        {
            _db = db;
        }

        public async Task<TenantDto> CreateTenantAsync(TenantCreateDto dto)
        {
            var tenant = new Tenant { Name = dto.Name };
            _db.Tenants.Add(tenant);

            if (dto.Domains != null)
            {
                foreach (var d in dto.Domains)
                {
                    _db.TenantDomains.Add(new TenantDomain
                    {
                        TenantId = tenant.Id,
                        Domain = d,
                        ValidationMethod = "manual"
                    });
                }
            }

            await _db.SaveChangesAsync();

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Domains = dto.Domains ?? new List<string>(),
                CreatedAt = tenant.CreatedAt,
                IsActive = tenant.IsActive
            };
        }

        public async Task<TenantDto> GetTenantAsync(Guid tenantId)
        {
            var t = await _db.Tenants.FindAsync(tenantId);
            if (t == null) return null;
            var domains = await _db.TenantDomains.Where(d => d.TenantId == tenantId).Select(d => d.Domain).ToListAsync();
            return new TenantDto { Id = t.Id, Name = t.Name, Domains = domains, CreatedAt = t.CreatedAt, IsActive = t.IsActive };
        }

        public async Task<IEnumerable<TenantDto>> ListTenantsAsync(int page = 1, int pageSize = 50)
        {
            var tenants = await _db.Tenants.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = new List<TenantDto>();
            foreach (var t in tenants)
            {
                var domains = await _db.TenantDomains.Where(d => d.TenantId == t.Id).Select(d => d.Domain).ToListAsync();
                result.Add(new TenantDto { Id = t.Id, Name = t.Name, Domains = domains, CreatedAt = t.CreatedAt, IsActive = t.IsActive });
            }
            return result;
        }

        public async Task<UserDto> CreateUserAsync(Guid tenantId, UserCreateDto dto)
        {
            var user = new User { TenantId = tenantId, Email = dto.Email, DisplayName = dto.DisplayName, Role = Enum.Parse<TenantRole>(dto.Role) };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return new UserDto { Id = user.Id, TenantId = user.TenantId, Email = user.Email, DisplayName = user.DisplayName, Role = user.Role.ToString(), CreatedAt = user.CreatedAt, IsActive = user.IsActive };
        }

        public async Task<IEnumerable<UserDto>> ListUsersAsync(Guid tenantId)
        {
            return await _db.Users.Where(u => u.TenantId == tenantId).Select(u => new UserDto { Id = u.Id, TenantId = u.TenantId, Email = u.Email, DisplayName = u.DisplayName, Role = u.Role.ToString(), CreatedAt = u.CreatedAt, IsActive = u.IsActive }).ToListAsync();
        }

        public async Task<Guid?> ResolveTenantByHostAsync(string host)
        {
            var domain = await _db.TenantDomains.FirstOrDefaultAsync(d => d.Domain == host);
            return domain?.TenantId;
        }
    }
}