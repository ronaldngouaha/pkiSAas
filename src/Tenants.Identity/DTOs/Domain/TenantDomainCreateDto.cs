namespace Acme.Pki.Tenants.Identity.DTOs.Domain
{
    public class TenantDomainCreateDto
    {
        public required string Domain { get; set; }
        public required string ValidationMethod { get; set; } // "dns" or "http"
    }
}
