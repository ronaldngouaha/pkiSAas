namespace Acme.Pki.Tenants.Identity.DTOs
{
    public class ApiEnvelopeDto
    {
        public int statuscode { get; set; }
        public object? data { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
