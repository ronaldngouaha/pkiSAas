using System;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}