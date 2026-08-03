using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Security.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Acme.Pki.Tenants.Identity.Security.Handlers
{
    public class ScopeRestrictedHandler : AuthorizationHandler<ScopeRestrictedRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRestrictedRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == "roles" && c.Value == "SuperAdmin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var scope = context.User.FindFirst("scope")?.Value
                ?? context.User.FindFirst("scp")?.Value;

            if (string.IsNullOrWhiteSpace(scope) || scope.Trim() == "*")
            {
                context.Fail();
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
