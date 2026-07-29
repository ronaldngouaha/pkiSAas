using System;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool MfaEnabled { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public string Metadata { get; set; }
    }
}