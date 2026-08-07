namespace Acme.Pki.Tenants.Identity.DTOs.Observability
{
    public class ObservabilityRabbitResponseDto
    {
        public string CorrelationId { get; set; } = string.Empty;
        public ObservabilityRabbitLinksDto Links { get; set; } = new();
        public ObservabilityRabbitServiceDto RabbitService { get; set; } = new();
    }

    public class ObservabilityRabbitLinksDto
    {
        public string Self { get; set; } = string.Empty;
    }

    public class ObservabilityRabbitServiceDto
    {
        public string Status { get; set; } = "unknown";
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string Queue { get; set; } = "audit.events";
        public string Exchange { get; set; } = "audit.exchange";
        public bool TcpReachable { get; set; }
        public long AuditPublishFailureTotal { get; set; }
    }
}