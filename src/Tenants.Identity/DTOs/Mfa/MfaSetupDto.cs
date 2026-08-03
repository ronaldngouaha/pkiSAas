namespace Acme.Pki.Tenants.Identity.DTOs.Mfa
{
    public class MfaSetupDto
    {
        public byte[] QrCodePng { get; set; } = Array.Empty<byte>();
        public string ManualEntryKey { get; set; } = string.Empty; // base32
    }
}
