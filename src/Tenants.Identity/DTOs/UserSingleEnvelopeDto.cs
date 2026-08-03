namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class UserSingleEnvelopeDto
    {
        public int statuscode { get; set; }
        public UserDto? data { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
