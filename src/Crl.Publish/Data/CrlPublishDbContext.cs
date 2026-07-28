using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Crl.Publish.Data;

public class CrlPublishDbContext : DbContext
{
    public CrlPublishDbContext(DbContextOptions<CrlPublishDbContext> options)
        : base(options)
    {
    }
}
