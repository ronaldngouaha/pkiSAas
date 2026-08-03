namespace Acme.Pki.Tenants.Identity.DTOs.Roles
{
    public class RoleSingleEnvelopeDto
    {
        public int statuscode { get; set; }
        public RoleDefinitionDto? data { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
