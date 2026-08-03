using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.DTOs.Roles
{
    public class RoleListEnvelopeDto
    {
        public int statuscode { get; set; }
        public IEnumerable<RoleDefinitionDto> data { get; set; } = new List<RoleDefinitionDto>();
        public string message { get; set; } = string.Empty;
    }
}
