using System;

namespace Acme.Pki.Tenants.Identity.Models
{
    public enum TenantRole { TenantAdmin, User, Viewer }

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public TenantRole Role { get; set; } = TenantRole.User;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}