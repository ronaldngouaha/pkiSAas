namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class RegisterRequestDto
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}