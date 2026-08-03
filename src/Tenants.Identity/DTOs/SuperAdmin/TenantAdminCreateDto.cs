namespace Acme.Pki.Tenants.Identity.DTOs.SuperAdmin
{
    public class TenantAdminCreateDto
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public string Metadata { get; set; }
    }
}
