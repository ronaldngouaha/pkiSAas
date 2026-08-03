using System;

namespace Acme.Pki.Tenants.Identity.DTOs.Roles
{
    public class CreateRoleDefinitionDto
    {
        public Guid? TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RoleMap { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Attributes { get; set; }
    }
}
