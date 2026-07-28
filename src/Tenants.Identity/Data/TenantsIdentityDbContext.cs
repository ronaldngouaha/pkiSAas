using Microsoft.EntityFrameworkCore;
using Acme.Pki.Tenants.Identity.Models;

namespace Acme.Pki.Tenants.Identity.Data
{
    public class TenantsIdentityDbContext : DbContext
    {
        public TenantsIdentityDbContext(DbContextOptions<TenantsIdentityDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantDomain> TenantDomains { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>().HasKey(t => t.Id);
            modelBuilder.Entity<TenantDomain>().HasKey(d => d.Id);
            modelBuilder.Entity<User>().HasKey(u => u.Id);
            modelBuilder.Entity<RefreshToken>().HasKey(r => r.Id);

            modelBuilder.Entity<User>()
                .HasIndex(u => new { u.TenantId, u.Email })
                .IsUnique();

            modelBuilder.Entity<TenantDomain>()
                .HasIndex(d => d.Domain)
                .IsUnique(false);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.UserId);
        }
    }
}
