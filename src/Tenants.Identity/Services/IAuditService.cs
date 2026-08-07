using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class AuditEventSnapshot
    {
        public string EventType { get; set; } = string.Empty;
        public Guid? TenantId { get; set; }
        public Guid? ActorUserId { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }

    public class AuditServiceSnapshot
    {
        public long TotalPublishedEvents { get; set; }
        public DateTime? LastPublishedAtUtc { get; set; }
        public IList<AuditEventSnapshot> RecentEvents { get; set; } = new List<AuditEventSnapshot>();
    }

    public interface IAuditService
    {
        Task PublishAsync(string eventType, Guid? tenantId, Guid? actorUserId, IDictionary<string, string> data);
        AuditServiceSnapshot GetSnapshot();
    }
}
