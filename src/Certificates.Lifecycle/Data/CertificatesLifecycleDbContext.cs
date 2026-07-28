using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Certificates.Lifecycle.Data;

public class CertificatesLifecycleDbContext : DbContext
{
    public CertificatesLifecycleDbContext(DbContextOptions<CertificatesLifecycleDbContext> options)
        : base(options)
    {
    }
}
