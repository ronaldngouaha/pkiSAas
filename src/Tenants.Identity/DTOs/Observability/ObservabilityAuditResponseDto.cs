using System;
using System.Collections.Generic;

namespace Acme.Pki.Tenants.Identity.DTOs.Observability
{
    public class ObservabilityAuditResponseDto
    {
        public string CorrelationId { get; set; } = string.Empty;
        public ObservabilityAuditLinksDto Links { get; set; } = new();
        public ObservabilityAuditServiceDto AuditService { get; set; } = new();
    }

    public class ObservabilityAuditLinksDto
    {
        public string Self { get; set; } = string.Empty;
    }

    public class ObservabilityAuditServiceDto
    {
        public long TotalPublishedEvents { get; set; }
        public int FilteredEventsCount { get; set; }
        public DateTime? LastPublishedAtUtc { get; set; }
        public IList<ObservabilityAuditEventDto> RecentEvents { get; set; } = new List<ObservabilityAuditEventDto>();
    }

    public class ObservabilityAuditEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public Guid? TenantId { get; set; }
        public Guid? ActorUserId { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}