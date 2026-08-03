using System;
using System.Linq;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.DTOs;
using Acme.Pki.Tenants.Identity.DTOs.SuperAdmin;
using Acme.Pki.Tenants.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class ServiceTests
    {
        private static IConfiguration CreateConfiguration(string defaultTenantUserPassword = "TenantUser@123")
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["Security:DefaultTenantUserPassword"] = defaultTenantUserPassword
                })
                .Build();
        }

        private static TenantsIdentityDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<TenantsIdentityDbContext>()
                .UseInMemoryDatabase($"ServiceTests-{Guid.NewGuid()}")
                .Options;
            return new TenantsIdentityDbContext(options);
        }

        [Fact]
        public async Task TenantService_CreateAndGet_ShouldWork()
        {
            using var db = CreateDbContext();
            var service = new TenantService(db);
            var createdBy = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var created = await service.CreateAsync(new TenantCreateDto
            {
                Name = "Acme",
                Slug = "acme",
                PrimaryDomain = "acme.test",
                PlanTier = "Standard",
                MaxCertificates = 100,
                Metadata = "{}",
                Domains = new() { "acme.test" }
            }, createdBy);

            var fetched = await service.GetAsync(created.Id);

            Assert.NotNull(fetched);
            Assert.Equal("Acme", fetched.Name);
            Assert.Contains("acme.test", fetched.Domains);
            Assert.Equal(createdBy, fetched.CreatedBy);
        }

        [Fact]
        public async Task UserService_CreateAndList_ShouldWork()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "Tenant",
                Slug = "tenant",
                PrimaryDomain = "tenant.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var service = new UserService(db);
            await service.CreateAsync(tenant.Id, new UserCreateDto
            {
                Email = "user@tenant.test",
                DisplayName = "User",
                Password = "P@ssw0rd123!",
                Role = new[] { "User" },
                Metadata = "{}"
            });

            var users = (await service.ListAsync(tenant.Id)).ToList();
            Assert.Single(users);
            Assert.Equal("user@tenant.test", users[0].Email);
            Assert.False(users[0].MfaEnabled);
        }

        [Fact]
        public async Task DomainService_ResolveValidatedHost_ShouldReturnTenantId()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "DomainTenant",
                Slug = "domaintenant",
                PrimaryDomain = "domain.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var service = new DomainService(db);
            await service.AddDomainAsync(tenant.Id, "domain.test");
            await service.ValidateDomainAsync(tenant.Id, "domain.test", "ok");

            var resolved = await service.ResolveTenantByHostAsync("domain.test");
            Assert.Equal(tenant.Id, resolved);
        }

        [Fact]
        public async Task MfaService_BeginTotpSetup_ShouldKeepMfaDisabledUntilVerify()
        {
            using var db = CreateDbContext();
            var user = new Models.User
            {
                Email = "admin@tenant.test",
                NormalizedEmail = "admin@tenant.test",
                DisplayName = "Admin",
                Username = "admin@tenant.test",
                Role = Models.TenantRole.SuperAdmin,
                PasswordHash = "hash",
                EmailVerificationTokenHash = string.Empty,
                MfaEnabled = false,
                MfaMethods = "[]",
                IsActive = true,
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1"
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new MfaService(db, new FakeKeyEncryptionService());

            var setup = await service.BeginTotpSetupAsync(user.Id);
            var reloadedUser = await db.Users.FindAsync(user.Id);

            Assert.NotNull(setup.QrCodePng);
            Assert.NotEmpty(setup.QrCodePng);
            Assert.False(string.IsNullOrWhiteSpace(setup.ManualEntryKey));
            Assert.NotNull(reloadedUser);
            Assert.False(reloadedUser!.MfaEnabled);
        }

        [Fact]
        public async Task TenantService_Suspend_ShouldReturnSuspendedTenant()
        {
            using var db = CreateDbContext();
            var service = new TenantService(db);
            var createdBy = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var created = await service.CreateAsync(new TenantCreateDto
            {
                Name = "SuspendMe",
                Slug = "suspendme",
                PrimaryDomain = "suspend.test",
                PlanTier = "Free",
                Metadata = "{}",
                Domains = new() { "suspend.test" }
            }, createdBy);

            var suspended = await service.SuspendAsync(created.Id, "Test suspension");

            Assert.NotNull(suspended);
            Assert.Equal(created.Id, suspended.Id);
            Assert.True(suspended.IsSuspended);
        }

        [Fact]
        public async Task UserService_ChangePassword_ShouldUpdateHash()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "PwdTenant",
                Slug = "pwdtenant",
                PrimaryDomain = "pwd.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var user = new Models.User
            {
                TenantId = tenant.Id,
                Email = "pwd.user@test.local",
                NormalizedEmail = "pwd.user@test.local",
                DisplayName = "Pwd User",
                Username = "pwd.user@test.local",
                Role = Models.TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db);
            await service.ChangePasswordAsync(tenant.Id, user.Id, "NewPass123!");

            var reloaded = await db.Users.FindAsync(user.Id);
            Assert.NotNull(reloaded);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPass123!", reloaded!.PasswordHash));
        }

        [Fact]
        public async Task SuperAdminService_ChangePassword_ShouldUpdateHash()
        {
            using var db = CreateDbContext();
            var superAdmin = new Models.User
            {
                TenantId = null,
                Email = "root.password@test.local",
                NormalizedEmail = "root.password@test.local",
                DisplayName = "Root",
                Username = "root.password@test.local",
                Role = Models.TenantRole.SuperAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldRoot123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            };
            db.Users.Add(superAdmin);
            await db.SaveChangesAsync();

            var service = new SuperAdminService(db, CreateConfiguration());
            await service.ChangePasswordAsync(superAdmin.Id, "NewRoot123!");

            var reloaded = await db.Users.FindAsync(superAdmin.Id);
            Assert.NotNull(reloaded);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewRoot123!", reloaded!.PasswordHash));
        }

        [Fact]
        public async Task UserService_Update_ShouldUpdateUserData()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "UpdateTenant",
                Slug = "updatetenant",
                PrimaryDomain = "update.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var user = new Models.User
            {
                TenantId = tenant.Id,
                Email = "old.user@test.local",
                NormalizedEmail = "old.user@test.local",
                DisplayName = "Old User",
                Username = "old.user@test.local",
                Role = Models.TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("UserPass123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db);
            var updated = await service.UpdateAsync(tenant.Id, user.Id, new UserUpdateDto
            {
                Email = "new.user@test.local",
                DisplayName = "New User",
                Role = new[] { "Viewer" },
                Metadata = "{\"source\":\"test\"}"
            });

            Assert.NotNull(updated);
            Assert.Equal("new.user@test.local", updated.Email);
            Assert.Equal("New User", updated.DisplayName);
            Assert.Equal(new[] { "Viewer" }, updated.Role);
            Assert.Equal("{\"source\":\"test\"}", updated.Metadata);
        }

        [Fact]
        public async Task SuperAdminService_Update_ShouldUpdateSuperAdminData()
        {
            using var db = CreateDbContext();
            var superAdmin = new Models.User
            {
                TenantId = null,
                Email = "old.root@test.local",
                NormalizedEmail = "old.root@test.local",
                DisplayName = "Old Root",
                Username = "old.root@test.local",
                Role = Models.TenantRole.SuperAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldRoot123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            };
            db.Users.Add(superAdmin);
            await db.SaveChangesAsync();

            var service = new SuperAdminService(db, CreateConfiguration());
            var updated = await service.UpdateAsync(superAdmin.Id, new SuperAdminUpdateDto
            {
                Email = "new.root@test.local",
                DisplayName = "New Root",
                Metadata = "{\"owner\":\"security\"}"
            });

            Assert.NotNull(updated);
            Assert.Equal("new.root@test.local", updated!.Email);
            Assert.Equal("New Root", updated.DisplayName);
            Assert.Equal("{\"owner\":\"security\"}", updated.Metadata);
        }

        [Fact]
        public async Task SuperAdminService_CreateTenantAdmin_ShouldCreateTenantAdminOnly()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "SaTenant",
                Slug = "satenant",
                PrimaryDomain = "sa.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var service = new SuperAdminService(db, CreateConfiguration());
            var created = await service.CreateTenantAdminAsync(tenant.Id, new TenantAdminCreateDto
            {
                Email = "tenant.admin@test.local",
                DisplayName = "Tenant Admin",
                Password = "Admin123$",
                Metadata = "{}"
            });

            Assert.Equal(tenant.Id, created.TenantId);
            Assert.Equal(new[] { "TenantAdmin" }, created.Role);
        }

        [Fact]
        public async Task SuperAdminService_ResetTenantUserPasswordToDefault_ShouldResetPassword()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "ResetTenant",
                Slug = "resettenant",
                PrimaryDomain = "reset.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var user = new Models.User
            {
                TenantId = tenant.Id,
                Email = "reset.user@test.local",
                NormalizedEmail = "reset.user@test.local",
                DisplayName = "Reset User",
                Username = "reset.user@test.local",
                Role = Models.TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = false,
                FailedLoginCount = 7,
                LockoutUntil = DateTime.UtcNow.AddHours(1)
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new SuperAdminService(db, CreateConfiguration("DefaultPwd123$"));
            var defaultPassword = await service.ResetTenantUserPasswordToDefaultAsync(tenant.Id, user.Id);

            var reloaded = await db.Users.FindAsync(user.Id);
            Assert.NotNull(reloaded);
            Assert.Equal("DefaultPwd123$", defaultPassword);
            Assert.True(BCrypt.Net.BCrypt.Verify("DefaultPwd123$", reloaded!.PasswordHash));
            Assert.True(reloaded.IsActive);
            Assert.Equal(0, reloaded.FailedLoginCount);
            Assert.Null(reloaded.LockoutUntil);
        }

        [Fact]
        public async Task SuperAdminService_ListTenantUsers_ShouldReturnTenantUsers()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "ListTenant",
                Slug = "listtenant",
                PrimaryDomain = "list.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            db.Users.Add(new Models.User
            {
                TenantId = tenant.Id,
                Email = "a@test.local",
                NormalizedEmail = "a@test.local",
                DisplayName = "A",
                Username = "a@test.local",
                Role = Models.TenantRole.User,
                PasswordHash = "hash",
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            });

            db.Users.Add(new Models.User
            {
                TenantId = tenant.Id,
                Email = "b@test.local",
                NormalizedEmail = "b@test.local",
                DisplayName = "B",
                Username = "b@test.local",
                Role = Models.TenantRole.Viewer,
                PasswordHash = "hash",
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            });

            await db.SaveChangesAsync();

            var service = new SuperAdminService(db, CreateConfiguration());
            var users = (await service.ListTenantUsersAsync(tenant.Id)).ToList();

            Assert.Equal(2, users.Count);
            Assert.All(users, u => Assert.Equal(tenant.Id, u.TenantId));
        }

        [Fact]
        public async Task UserService_AddRole_ShouldUpdateRole()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "RoleTenant",
                Slug = "roletenant",
                PrimaryDomain = "role.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var user = new Models.User
            {
                TenantId = tenant.Id,
                Email = "role.user@test.local",
                NormalizedEmail = "role.user@test.local",
                DisplayName = "Role User",
                Username = "role.user@test.local",
                Role = Models.TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("UserPass123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db);
            var updated = await service.AddRoleAsync(tenant.Id, user.Id, "Viewer");

            Assert.NotNull(updated);
            Assert.Equal(new[] { "User", "Viewer" }, updated.Role);
        }

        [Fact]
        public async Task UserService_AddRole_SuperAdminShouldFail()
        {
            using var db = CreateDbContext();
            var tenant = new Models.Tenant
            {
                Name = "RoleTenant2",
                Slug = "roletenant2",
                PrimaryDomain = "role2.test",
                PlanTier = "Free",
                Metadata = "{}",
                OwnerContactEmail = string.Empty,
                BillingAccountId = string.Empty,
                Region = "global",
                DefaultAuthPolicy = "Internal"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var user = new Models.User
            {
                TenantId = tenant.Id,
                Email = "role2.user@test.local",
                NormalizedEmail = "role2.user@test.local",
                DisplayName = "Role User 2",
                Username = "role2.user@test.local",
                Role = Models.TenantRole.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("UserPass123!"),
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddRoleAsync(tenant.Id, user.Id, "SuperAdmin"));
        }

        private sealed class FakeKeyEncryptionService : IKeyEncryptionService
        {
            public Task<(string Encrypted, string KeyId)> EncryptAsync(string plaintext)
            {
                return Task.FromResult((plaintext, "test-key"));
            }

            public Task<string> DecryptAsync(string encrypted, string keyId)
            {
                return Task.FromResult(encrypted);
            }
        }
    }
}
