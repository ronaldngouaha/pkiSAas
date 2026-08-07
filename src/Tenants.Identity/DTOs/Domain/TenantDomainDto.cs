using System;

namespace Acme.Pki.Tenants.Identity.DTOs.Domain
{
    public class TenantDomainDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public required string Domain { get; set; }
        public bool IsValidated { get; set; }
        public required string ValidationMethod { get; set; }
        public string? Challenge { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
