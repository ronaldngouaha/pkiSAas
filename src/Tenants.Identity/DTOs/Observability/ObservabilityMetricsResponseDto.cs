using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.DTOs.Observability
{
    public class ObservabilityMetricsResponseDto
    {
        public string CorrelationId { get; set; } = string.Empty;
        public ObservabilityLinksDto Links { get; set; } = new();
        public ObservabilityMetricsDto Metrics { get; set; } = new();
    }

    public class ObservabilityLinksDto
    {
        public string Self { get; set; } = string.Empty;
        public string Rabbit { get; set; } = string.Empty;
        public string Audit { get; set; } = string.Empty;
    }

    public class ObservabilityMetricsDto
    {
        public long LoginSuccessTotal { get; set; }
        public long LoginFailureTotal { get; set; }
        public long LoginAttemptTotal { get; set; }
        public long RefreshFailureTotal { get; set; }
        public long MfaFailureTotal { get; set; }
        public long TokenReplayAttemptTotal { get; set; }
        public long KeyRotationFailureTotal { get; set; }
        public long AuditPublishFailureTotal { get; set; }
        public long CrudActionTotal { get; set; }
        public long CrudCreateTotal { get; set; }
        public long CrudReadTotal { get; set; }
        public long CrudUpdateTotal { get; set; }
        public long CrudDeleteTotal { get; set; }
        public IDictionary<string, long> CrudActionsByKey { get; set; } = new Dictionary<string, long>();
        public IDictionary<string, long> LoginFailuresByReason { get; set; } = new Dictionary<string, long>();
        public IDictionary<string, long> RefreshFailuresByReason { get; set; } = new Dictionary<string, long>();
        public IDictionary<string, long> MfaFailuresByReason { get; set; } = new Dictionary<string, long>();
        public IDictionary<string, long> KeyRotationFailuresByReason { get; set; } = new Dictionary<string, long>();
        public IDictionary<string, long> AuditPublishFailuresByReason { get; set; } = new Dictionary<string, long>();
    }
}
