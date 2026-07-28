using System;

namespace Acme.Pki.Tenants.Identity.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty; // store hashed token
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedByIp { get; set; } = string.Empty;
        public DateTime? RevokedAt { get; set; }
        public string RevokedByIp { get; set; } = string.Empty;
        public string ReplacedByTokenHash { get; set; } = string.Empty;
    }
}
