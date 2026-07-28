namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class UserCreateDto
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
    }
}