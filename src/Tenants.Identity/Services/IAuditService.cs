using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IAuditService
    {
        Task PublishAsync(string eventType, Guid? tenantId, Guid? actorUserId, IDictionary<string, string> data);
    }
}
