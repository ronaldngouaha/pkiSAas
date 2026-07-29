using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class TenantCreateDto
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string PrimaryDomain { get; set; }
        public string PlanTier { get; set; }
        public int? MaxCertificates { get; set; }
        public string Metadata { get; set; }
        public List<string> Domains { get; set; }
    }
}