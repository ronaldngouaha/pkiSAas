using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Security.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Acme.Pki.Tenants.Identity.Security.Handlers
{
    public class TenantAdminHandler : AuthorizationHandler<TenantAdminRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantAdminHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantAdminRequirement requirement)
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

            var hasRole = context.User.HasClaim(c => c.Type == "roles" && c.Value == "TenantAdmin");
            if (!hasRole)
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
