using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Pki.Core.Data;

public class PkiCoreDbContext : DbContext
{
    public PkiCoreDbContext(DbContextOptions<PkiCoreDbContext> options)
        : base(options)
    {
    }
}
