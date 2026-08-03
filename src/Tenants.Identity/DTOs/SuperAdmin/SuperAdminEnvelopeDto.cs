namespace Acme.Pki.Tenants.Identity.DTOs.SuperAdmin
{
    public class SuperAdminEnvelopeDto
    {
        public int statuscode { get; set; }
        public object? data { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
