using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Admin.Pki.Data;

public class AdminPkiDbContext : DbContext
{
    public AdminPkiDbContext(DbContextOptions<AdminPkiDbContext> options)
        : base(options)
    {
    }
}
