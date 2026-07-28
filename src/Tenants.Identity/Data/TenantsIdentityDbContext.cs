using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Tenants.Identity.Data;

public class TenantsIdentityDbContext : DbContext
{
    public TenantsIdentityDbContext(DbContextOptions<TenantsIdentityDbContext> options)
        : base(options)
    {
    }
}
