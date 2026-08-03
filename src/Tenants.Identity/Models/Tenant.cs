using System;
using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.Models
{
    public class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Slug { get; set; } // url friendly unique
        public string PrimaryDomain { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsSuspended { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // soft delete
        public string OwnerContactEmail { get; set; }
        public string BillingAccountId { get; set; }
        public string PlanTier { get; set; } // Free, Standard, Enterprise
        public int? MaxCertificates { get; set; } // example quota
        public string Region { get; set; } // data residency hint
        public string DefaultAuthPolicy { get; set; } // Internal, AzureAD, ExternalIdP
        public string Metadata { get; set; } // JSON extensible
        public ICollection<TenantDomain> Domains { get; set; } = new List<TenantDomain>();
    }
}