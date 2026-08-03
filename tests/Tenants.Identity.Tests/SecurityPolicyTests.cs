using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Models;
using Acme.Pki.Tenants.Identity.Security.Handlers;
using Acme.Pki.Tenants.Identity.Security.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acme.Pki.Tenants.Identity.Tests
{
    public class SecurityPolicyTests
    {
        [Fact]
        public async Task TenantAdminHandler_Allows_When_TidMatchesRoute()
        {
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            var provider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext();
            var tenantId = Guid.NewGuid();
            httpContext.Request.RouteValues["tenantId"] = tenantId.ToString();
            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = httpContext;

            var requirement = new TenantAdminRequirement();
            var handler = new TenantAdminHandler(accessor);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim("roles", "TenantAdmin"),
                new Claim("tid", tenantId.ToString()),
                new Claim("sub", Guid.NewGuid().ToString())
            }, "test");

            var user = new ClaimsPrincipal(identity);
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task MfaHandler_Fails_When_UserHasMfaEnabledButTokenLacksMfaClaim()
        {
            var options = new DbContextOptionsBuilder<TenantsIdentityDbContext>()
                .UseInMemoryDatabase($"MfaHandlerTestDb-{Guid.NewGuid()}")
                .Options;

            await using var db = new TenantsIdentityDbContext(options);
            var userId = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = userId,
                Email = "u@test.local",
                NormalizedEmail = "u@test.local",
                DisplayName = "User",
                Username = "u@test.local",
                Role = TenantRole.User,
                PasswordHash = "hash",
                EmailVerificationTokenHash = string.Empty,
                MfaMethods = "[]",
                PreferredLocale = "fr-FR",
                Timezone = "UTC",
                PhoneNumber = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Metadata = "{}",
                ConsentVersion = "v1",
                MfaEnabled = true,
                IsActive = true
            });
            await db.SaveChangesAsync();

            var handler = new MfaHandler(db);
            var requirement = new MfaRequirement();

            var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "test");
            var user = new ClaimsPrincipal(identity);
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task OwnResourceHandler_Allows_When_SubMatchesRouteUserId()
        {
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            var provider = services.BuildServiceProvider();

            var userId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues["userId"] = userId.ToString();
            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = httpContext;

            var requirement = new OwnResourceRequirement();
            var handler = new OwnResourceHandler(accessor);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("roles", "EndUser")
            }, "test");

            var user = new ClaimsPrincipal(identity);
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task ReadOnlyHandler_Fails_On_Post_Request()
        {
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            var provider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = HttpMethods.Post;
            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = httpContext;

            var requirement = new ReadOnlyRequirement();
            var handler = new ReadOnlyHandler(accessor);

            var identity = new ClaimsIdentity(new[] { new Claim("roles", "Viewer") }, "test");
            var user = new ClaimsPrincipal(identity);
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }
    }
}