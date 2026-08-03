using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Security.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Acme.Pki.Tenants.Identity.Security.Handlers
{
    public class OwnResourceHandler : AuthorizationHandler<OwnResourceRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OwnResourceHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnResourceRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == "roles" && c.Value == "SuperAdmin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var sub = context.User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
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

            if (httpContext.Request.RouteValues.TryGetValue("userId", out var routeUserIdObj) &&
                Guid.TryParse(routeUserIdObj?.ToString(), out var routeUserId) &&
                routeUserId == userId)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            context.Fail();
            return Task.CompletedTask;
        }
    }
}
