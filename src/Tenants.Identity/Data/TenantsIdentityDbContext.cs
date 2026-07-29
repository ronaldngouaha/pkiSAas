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
            // Tenant
            modelBuilder.Entity<Tenant>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Name).IsRequired().HasMaxLength(200);
                b.Property(t => t.Slug).HasMaxLength(100);
                b.HasIndex(t => t.Slug).IsUnique();
                b.HasIndex(t => t.PrimaryDomain).IsUnique(false);
                b.Property(t => t.Metadata).HasColumnType("nvarchar(max)");
            });

            // TenantDomain
            modelBuilder.Entity<TenantDomain>(b =>
            {
                b.HasKey(d => d.Id);
                b.HasIndex(d => d.Domain);
                b.HasOne<Tenant>().WithMany(t => t.Domains).HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Cascade);
            });

            // User
            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);
                b.Property(u => u.Email).IsRequired().HasMaxLength(254);
                b.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(254);
                b.HasIndex(u => new { u.TenantId, u.NormalizedEmail }).IsUnique();
                b.HasIndex(u => u.Role);
                b.Property(u => u.MfaMethods).HasColumnType("nvarchar(max)");
                b.Property(u => u.Metadata).HasColumnType("nvarchar(max)");
                // filtered unique index for global SuperAdmin emails (TenantId IS NULL) can be added via raw SQL migration if needed
            });

            // RefreshToken (preserved for auth flows)
            modelBuilder.Entity<RefreshToken>(b =>
            {
                b.HasKey(r => r.Id);
                b.HasIndex(r => r.UserId);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
