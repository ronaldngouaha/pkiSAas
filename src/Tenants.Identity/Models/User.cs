using System;

namespace Acme.Pki.Tenants.Identity.Models
{
    public enum TenantRole { SuperAdmin, TenantAdmin, User, Viewer }

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; } // null for SuperAdmin
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public TenantRole Role { get; set; } = TenantRole.User;
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public bool IsActive { get; set; } = true;
    }
}