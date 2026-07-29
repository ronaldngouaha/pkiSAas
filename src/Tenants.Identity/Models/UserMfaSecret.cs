using System;

namespace Acme.Pki.Tenants.Identity.Models
{
    public class UserMfaSecret
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string EncryptedSecret { get; set; } = string.Empty; // encrypted with KMS/KeyVault
        public string KeyId { get; set; } = string.Empty; // key identifier used to encrypt
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
    }
}
