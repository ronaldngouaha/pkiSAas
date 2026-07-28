using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Audit.Events.Data;

public class AuditEventsDbContext : DbContext
{
    public AuditEventsDbContext(DbContextOptions<AuditEventsDbContext> options)
        : base(options)
    {
    }
}
