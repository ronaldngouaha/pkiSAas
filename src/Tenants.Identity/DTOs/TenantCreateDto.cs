using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class TenantCreateDto
    {
        public string Name { get; set; }
        public List<string> Domains { get; set; }
    }
}