using System;
using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<string> Domains { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}