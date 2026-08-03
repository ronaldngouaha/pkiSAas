using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Security.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Acme.Pki.Tenants.Identity.Security.Handlers
{
    public class TenantScopeHandler : AuthorizationHandler<TenantScopeRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantScopeHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantScopeRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == "roles" && c.Value == "SuperAdmin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var tidClaim = context.User.FindFirst("tid")?.Value;
            if (string.IsNullOrWhiteSpace(tidClaim) || !Guid.TryParse(tidClaim, out var tokenTid))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (httpContext.Items.TryGetValue("TenantId", out var tenantItem) && tenantItem is Guid requestTid && requestTid == tokenTid)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var routeTidObj) &&
                Guid.TryParse(routeTidObj?.ToString(), out var routeTid) &&
                routeTid == tokenTid)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            context.Fail();
            return Task.CompletedTask;
        }
    }
}
