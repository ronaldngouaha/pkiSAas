namespace Acme.Pki.Tenants.Identity.Options
{
    public class JwtOptions
    {
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public required string AccessTokenMinutes { get; set; }
        public required string RefreshTokenDays { get; set; }
    }
}
