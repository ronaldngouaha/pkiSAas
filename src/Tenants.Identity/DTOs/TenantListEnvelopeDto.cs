using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class TenantListEnvelopeDto
    {
        public int statuscode { get; set; }
        public IEnumerable<TenantDto> data { get; set; } = new List<TenantDto>();
        public string message { get; set; } = string.Empty;
    }
}
