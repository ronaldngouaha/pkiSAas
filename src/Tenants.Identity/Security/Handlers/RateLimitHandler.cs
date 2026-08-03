using System;
using System.Globalization;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Security.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Acme.Pki.Tenants.Identity.Security.Handlers
{
    public class RateLimitHandler : AuthorizationHandler<RateLimitRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RateLimitRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == "roles" && c.Value == "SuperAdmin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var exp = context.User.FindFirst("support_session_exp")?.Value;
            if (string.IsNullOrWhiteSpace(exp) ||
                !DateTime.TryParse(exp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var expiresAt) ||
                expiresAt <= DateTime.UtcNow)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
