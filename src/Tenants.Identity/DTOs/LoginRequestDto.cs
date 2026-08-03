namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class LoginRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? MfaCode { get; set; }
        public string? RecoveryCode { get; set; }
    }
}