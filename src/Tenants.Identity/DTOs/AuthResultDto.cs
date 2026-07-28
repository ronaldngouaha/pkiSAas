using System;

namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class AuthResultDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; } // raw token returned to client (store only hash)
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}