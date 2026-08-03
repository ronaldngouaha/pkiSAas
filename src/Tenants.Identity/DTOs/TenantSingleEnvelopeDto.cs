using System;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class TenantSingleEnvelopeDto
    {
        public int statuscode { get; set; }
        public TenantDto? data { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
