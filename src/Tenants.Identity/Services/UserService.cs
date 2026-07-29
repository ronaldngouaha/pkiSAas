using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class UserService : IUserService
    {
        private readonly TenantsIdentityDbContext _db;

        public UserService(TenantsIdentityDbContext db) => _db = db;

        public async Task<UserDto> CreateAsync(Guid tenantId, UserCreateDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var role = Enum.Parse<TenantRole>(dto.Role);
            var user = new User
            {
                TenantId = tenantId,
                Email = dto.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                DisplayName = dto.DisplayName,
                Username = normalizedEmail,
                Role = role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsEmailVerified = false,
                EmailVerificationTokenHash = string.Empty,
                MfaEnabled = dto.MfaEnabled,
                MfaMethods = dto.MfaEnabled ? "[\"totp\"]" : "[]",
                IsActive = true,
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                IsPhoneVerified = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = dto.Metadata ?? "{}",
                ServiceAccount = role == TenantRole.ServiceAccount,
                ConsentVersion = "v1"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Map(user);
        }

        public async Task<UserDto> GetAsync(Guid tenantId, Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId);
            return user == null ? null : Map(user);
        }

        public async Task<IEnumerable<UserDto>> ListAsync(Guid tenantId, int page = 1, int pageSize = 50)
        {
            var users = await _db.Users
                .Where(u => u.TenantId == tenantId)
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return users.Select(Map).ToList();
        }

        public async Task DeactivateAsync(Guid tenantId, Guid userId, string reason)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId);
            if (user == null) throw new KeyNotFoundException();
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            // publish audit event (AuditService) - to be wired by DI
        }

        public async Task ReactivateAsync(Guid tenantId, Guid userId, string reason)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId);
            if (user == null) throw new KeyNotFoundException();
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            // publish audit event (AuditService) - to be wired by DI
        }

        private static UserDto Map(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Email = user.Email,
                NormalizedEmail = user.NormalizedEmail,
                DisplayName = user.DisplayName,
                Role = user.Role.ToString(),
                IsEmailVerified = user.IsEmailVerified,
                MfaEnabled = user.MfaEnabled,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,
                Metadata = user.Metadata
            };
        }
    }
}
