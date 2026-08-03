using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Security.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Acme.Pki.Tenants.Identity.Security.Handlers
{
    public class ApprovalWorkflowHandler : AuthorizationHandler<ApprovalWorkflowRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApprovalWorkflowRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == "roles" && c.Value == "SuperAdmin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var approved = context.User.FindFirst("approval")?.Value
                ?? context.User.FindFirst("approved")?.Value;

            if (string.Equals(approved, "true", StringComparison.OrdinalIgnoreCase) || approved == "1")
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            context.Fail();
            return Task.CompletedTask;
        }
    }
}
