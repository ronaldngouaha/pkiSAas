using System;

namespace Acme.Pki.Tenants.Identity.DTOs.Roles
{
    public class RoleDefinitionDto
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RoleMap { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Attributes { get; set; } = "{}";
        public bool IsDefault { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
