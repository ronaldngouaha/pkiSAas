using System.Collections.Generic;
using Acme.Pki.Tenants.Identity.DTOs;

namespace Acme.Pki.Tenants.Identity.DTOs.SuperAdmin
{
    public class SuperAdminListEnvelopeDto
    {
        public int statuscode { get; set; }
        public IEnumerable<UserDto>? data { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
