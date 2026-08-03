using Acme.Pki.Tenants.Identity.DTOs;

namespace Acme.Pki.Tenants.Identity.DTOs.SuperAdmin
{
    public class SuperAdminCreateEnvelopeDto
    {
        public int statuscode { get; set; }
        public UserDto? data { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
