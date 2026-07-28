namespace Acme.Pki.Tenants.Identity.Options
{
    public class JwtOptions
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string AccessTokenMinutes { get; set; }
        public string RefreshTokenDays { get; set; }
    }
}
