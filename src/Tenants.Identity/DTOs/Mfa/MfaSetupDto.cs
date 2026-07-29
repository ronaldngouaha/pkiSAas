namespace Acme.Pki.Tenants.Identity.DTOs.Mfa
{
    public class MfaSetupDto
    {
        public string QrCodeBase64Png { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty; // base32
    }
}
