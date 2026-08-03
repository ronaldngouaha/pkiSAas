using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.DTOs.SuperAdmin;
using Acme.Pki.Tenants.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class SuperAdminService : ISuperAdminService
    {
        private readonly TenantsIdentityDbContext _db;
        private readonly IConfiguration _configuration;

        public SuperAdminService(TenantsIdentityDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<bool> AnyActiveSuperAdminAsync()
        {
            return await _db.Users.AnyAsync(u => u.TenantId == null && u.Role == TenantRole.SuperAdmin && u.IsActive);
        }

        public async Task<UserDto> CreateAsync(SuperAdminCreateDto dto)
        {
            var email = dto.Email.Trim();
            var normalizedEmail = email.ToLowerInvariant();

            var exists = await _db.Users.AnyAsync(u => u.TenantId == null && u.NormalizedEmail == normalizedEmail);
            if (exists)
            {
                throw new InvalidOperationException("A SuperAdmin with this email already exists.");
            }

            var user = new User
            {
                TenantId = null,
                Email = email,
                NormalizedEmail = normalizedEmail,
                DisplayName = dto.DisplayName.Trim(),
                Username = normalizedEmail,
                Role = TenantRole.SuperAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsEmailVerified = false,
                EmailVerificationTokenHash = string.Empty,
                MfaEnabled = false,
                MfaMethods = "[]",
                IsActive = true,
                FailedLoginCount = 0,
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                IsPhoneVerified = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ServiceAccount = false,
                ConsentVersion = "v1"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Map(user);
        }

        public async Task<UserDto> CreateTenantAdminAsync(Guid tenantId, TenantAdminCreateDto dto)
        {
            var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == tenantId && t.DeletedAt == null);
            if (!tenantExists)
            {
                throw new KeyNotFoundException();
            }

            var email = dto.Email.Trim();
            var normalizedEmail = email.ToLowerInvariant();

            var exists = await _db.Users.AnyAsync(u => u.TenantId == tenantId && u.NormalizedEmail == normalizedEmail);
            if (exists)
            {
                throw new InvalidOperationException("User already exists for this tenant.");
            }

            var user = new User
            {
                TenantId = tenantId,
                Email = email,
                NormalizedEmail = normalizedEmail,
                DisplayName = dto.DisplayName?.Trim() ?? string.Empty,
                Username = normalizedEmail,
                Role = TenantRole.TenantAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsEmailVerified = false,
                EmailVerificationTokenHash = string.Empty,
                MfaEnabled = false,
                MfaMethods = "[]",
                IsActive = true,
                FailedLoginCount = 0,
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                IsPhoneVerified = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = string.IsNullOrWhiteSpace(dto.Metadata) ? "{}" : dto.Metadata,
                ServiceAccount = false,
                ConsentVersion = "v1"
            };

            UserRoleResolver.SetRoles(user, new[] { TenantRole.TenantAdmin });

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Map(user);
        }

        public async Task<UserDto?> GetAsync(Guid id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == null && u.Role == TenantRole.SuperAdmin);
            return user == null ? null : Map(user);
        }

        public async Task<UserDto?> UpdateAsync(Guid id, SuperAdminUpdateDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == null && u.Role == TenantRole.SuperAdmin);
            if (user == null)
            {
                return null;
            }

            var email = dto.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email is required.");
            }

            var normalizedEmail = email.ToLowerInvariant();
            var exists = await _db.Users.AnyAsync(u => u.TenantId == null && u.Role == TenantRole.SuperAdmin && u.NormalizedEmail == normalizedEmail && u.Id != id);
            if (exists)
            {
                throw new InvalidOperationException("A SuperAdmin with this email already exists.");
            }

            user.Email = email;
            user.NormalizedEmail = normalizedEmail;
            user.Username = normalizedEmail;
            user.DisplayName = dto.DisplayName?.Trim() ?? string.Empty;
            user.Metadata = string.IsNullOrWhiteSpace(dto.Metadata) ? "{}" : dto.Metadata;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Map(user);
        }

        public async Task<IEnumerable<UserDto>> ListAsync(int page = 1, int pageSize = 50, bool includeInactive = false)
        {
            var query = _db.Users.Where(u => u.TenantId == null && u.Role == TenantRole.SuperAdmin);
            if (!includeInactive)
            {
                query = query.Where(u => u.IsActive);
            }

            var users = await query
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return users.Select(Map).ToList();
        }

        public async Task<IEnumerable<UserDto>> ListTenantUsersAsync(Guid tenantId, int page = 1, int pageSize = 50)
        {
            var users = await _db.Users
                .Where(u => u.TenantId == tenantId)
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return users.Select(Map).ToList();
        }

        public async Task<string> ResetTenantUserPasswordToDefaultAsync(Guid tenantId, Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            var defaultPassword = _configuration["Security:DefaultTenantUserPassword"]
                ?? _configuration["DEFAULT_TENANT_USER_PASSWORD"]
                ?? "TenantUser@123";

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.FailedLoginCount = 0;
            user.LockoutUntil = null;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return defaultPassword;
        }

        public async Task DeactivateAsync(Guid id, string reason)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == null && u.Role == TenantRole.SuperAdmin);
            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            if (user.IsActive)
            {
                var activeCount = await _db.Users.CountAsync(u => u.TenantId == null && u.Role == TenantRole.SuperAdmin && u.IsActive);
                if (activeCount <= 1)
                {
                    throw new InvalidOperationException("Cannot deactivate the last active SuperAdmin.");
                }
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task ReactivateAsync(Guid id, string reason)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == null && u.Role == TenantRole.SuperAdmin);
            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(Guid id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new InvalidOperationException("New password is required.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == null && u.Role == TenantRole.SuperAdmin);
            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword.Trim());
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
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
                Role = new[] { user.Role.ToString() },
                IsEmailVerified = user.IsEmailVerified,
                MfaEnabled = user.MfaEnabled,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,
                Metadata = UserRoleResolver.BuildPublicMetadata(user.Metadata)
            };
        }
    }
}
