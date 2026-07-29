using System;
using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string PrimaryDomain { get; set; }
        public string PlanTier { get; set; }
        public int? MaxCertificates { get; set; }
        public string Metadata { get; set; }
        public bool IsActive { get; set; }
        public bool IsSuspended { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Domains { get; set; }
    }
}