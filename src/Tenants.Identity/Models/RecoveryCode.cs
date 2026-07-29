using System;

namespace Acme.Pki.Tenants.Identity.Models
{
    public class RecoveryCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string CodeHash { get; set; } = string.Empty; // HMAC or bcrypt hash
        public bool Used { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }
    }
}
