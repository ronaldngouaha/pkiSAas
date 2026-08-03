using System;

namespace Acme.Pki.Tenants.Identity.Models
{
    public class RoleCatalog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string RoleMap { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Attributes { get; set; } = "{}";
        public bool IsDefault { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
