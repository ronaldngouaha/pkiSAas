using System;

namespace Acme.Pki.Tenants.Identity.Models
{
    public class TenantDomain
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Domain { get; set; }
        public bool IsValidated { get; set; } = false;
        public string ValidationMethod { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}