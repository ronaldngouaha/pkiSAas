using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class UserListEnvelopeDto
    {
        public int statuscode { get; set; }
        public IEnumerable<UserDto> data { get; set; } = new List<UserDto>();
        public string message { get; set; } = string.Empty;
    }
}
