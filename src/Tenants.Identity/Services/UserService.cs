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
            var requestedRoles = UserRoleResolver.ParseRequestedRoles(dto.Role);
            var primaryRole = requestedRoles.Contains(TenantRole.TenantAdmin)
                ? TenantRole.TenantAdmin
                : requestedRoles[0];
            var user = new User
            {
                TenantId = tenantId,
                Email = dto.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                DisplayName = dto.DisplayName,
                Username = normalizedEmail,
                Role = primaryRole,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsEmailVerified = false,
                EmailVerificationTokenHash = string.Empty,
                MfaEnabled = false,
                MfaMethods = "[]",
                IsActive = true,
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                IsPhoneVerified = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = string.IsNullOrWhiteSpace(dto.Metadata) ? "{}" : dto.Metadata,
                ServiceAccount = requestedRoles.Contains(TenantRole.ServiceAccount),
                ConsentVersion = "v1"
            };

            UserRoleResolver.SetRoles(user, requestedRoles);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Map(user);
        }

        public async Task<UserDto> GetAsync(Guid tenantId, Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId);
            return user == null ? null : Map(user);
        }

        public async Task<UserDto> UpdateAsync(Guid tenantId, Guid userId, UserUpdateDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId);
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
            var emailExists = await _db.Users.AnyAsync(u => u.TenantId == tenantId && u.NormalizedEmail == normalizedEmail && u.Id != userId);
            if (emailExists)
            {
                throw new InvalidOperationException("User already exists for this tenant.");
            }

            var requestedRoles = UserRoleResolver.ParseRequestedRoles(dto.Role);

            user.Email = email;
            user.NormalizedEmail = normalizedEmail;
            user.Username = normalizedEmail;
            user.DisplayName = dto.DisplayName?.Trim() ?? string.Empty;
            user.Metadata = string.IsNullOrWhiteSpace(dto.Metadata) ? user.Metadata : dto.Metadata;
            UserRoleResolver.SetRoles(user, requestedRoles);
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Map(user);
        }

        public async Task<UserDto> AddRoleAsync(Guid tenantId, Guid userId, string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new InvalidOperationException("Role is required.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            if (!UserRoleResolver.TryParseTenantRole(role, out var parsedRole))
            {
                throw new InvalidOperationException("Invalid role.");
            }

            if (parsedRole == TenantRole.SuperAdmin)
            {
                throw new InvalidOperationException("SuperAdmin role is not allowed for tenant users.");
            }

            var roles = UserRoleResolver.GetRoles(user).ToList();
            if (!roles.Contains(parsedRole))
            {
                roles.Add(parsedRole);
            }

            UserRoleResolver.SetRoles(user, roles);
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Map(user);
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

        public async Task ChangePasswordAsync(Guid tenantId, Guid userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new InvalidOperationException("New password is required.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId);
            if (user == null) throw new KeyNotFoundException();

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
                Role = UserRoleResolver.GetRoles(user).Select(r => r.ToString()).ToArray(),
                IsEmailVerified = user.IsEmailVerified,
                MfaEnabled = user.MfaEnabled,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,
                Metadata = UserRoleResolver.BuildPublicMetadata(user.Metadata)
            };
        }
    }
}
