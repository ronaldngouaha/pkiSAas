using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Domain.Validation.Data;

public class DomainValidationDbContext : DbContext
{
    public DomainValidationDbContext(DbContextOptions<DomainValidationDbContext> options)
        : base(options)
    {
    }
}
