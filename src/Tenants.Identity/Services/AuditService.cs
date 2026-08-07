using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> _logger;
        private readonly object _lock = new();
        private readonly Queue<AuditEventSnapshot> _recentEvents = new();
        private long _totalPublishedEvents;
        private DateTime? _lastPublishedAtUtc;
        private const int MaxRecentEvents = 100;

        public AuditService(ILogger<AuditService> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync(string eventType, Guid? tenantId, Guid? actorUserId, IDictionary<string, string> data)
        {
            var occurredAt = DateTime.UtcNow;

            _logger.LogInformation("Audit event {EventType} tenant={TenantId} actor={ActorUserId} data={@Data}", eventType, tenantId, actorUserId, data);

            var snapshotEvent = new AuditEventSnapshot
            {
                EventType = eventType,
                TenantId = tenantId,
                ActorUserId = actorUserId,
                OccurredAtUtc = occurredAt,
                Data = new Dictionary<string, string>(data)
            };

            lock (_lock)
            {
                _recentEvents.Enqueue(snapshotEvent);
                while (_recentEvents.Count > MaxRecentEvents)
                {
                    _recentEvents.Dequeue();
                }

                _lastPublishedAtUtc = occurredAt;
            }

            Interlocked.Increment(ref _totalPublishedEvents);
            return Task.CompletedTask;
        }

        public AuditServiceSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                return new AuditServiceSnapshot
                {
                    TotalPublishedEvents = Interlocked.Read(ref _totalPublishedEvents),
                    LastPublishedAtUtc = _lastPublishedAtUtc,
                    RecentEvents = _recentEvents
                        .Select(e => new AuditEventSnapshot
                        {
                            EventType = e.EventType,
                            TenantId = e.TenantId,
                            ActorUserId = e.ActorUserId,
                            OccurredAtUtc = e.OccurredAtUtc,
                            Data = new Dictionary<string, string>(e.Data)
                        })
                        .ToList()
                };
            }
        }
    }
}
