using System;

namespace Acme.Pki.Tenants.Identity.Models
{
    public enum TenantRole { SuperAdmin, TenantAdmin, User, Viewer, ServiceAccount }

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; } // null for SuperAdmin
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string DisplayName { get; set; }
        public string Username { get; set; }
        public TenantRole Role { get; set; } = TenantRole.User;
        public string PasswordHash { get; set; } // bcrypt/argon2
        public bool IsEmailVerified { get; set; } = false;
        public string EmailVerificationTokenHash { get; set; }
        public DateTime? EmailVerificationExpiresAt { get; set; }
        public bool MfaEnabled { get; set; } = false;
        public string MfaMethods { get; set; } // JSON
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public bool IsActive { get; set; } = true;
        public string PreferredLocale { get; set; }
        public string Timezone { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsPhoneVerified { get; set; }
        public string SecurityStamp { get; set; } // invalidate sessions on password change
        public string Metadata { get; set; } // JSON
        public bool ServiceAccount { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // soft delete
        public DateTime? PiiConsentGivenAt { get; set; }
        public string ConsentVersion { get; set; }
    }
}