using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> _logger;

        public AuditService(ILogger<AuditService> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync(string eventType, Guid? tenantId, Guid? actorUserId, IDictionary<string, string> data)
        {
            _logger.LogInformation("Audit event {EventType} tenant={TenantId} actor={ActorUserId} data={@Data}", eventType, tenantId, actorUserId, data);
            return Task.CompletedTask;
        }
    }
}
