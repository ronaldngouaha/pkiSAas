using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Security.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Tenants.Identity.Security.Handlers
{
    public class MfaHandler : AuthorizationHandler<MfaRequirement>
    {
        private readonly TenantsIdentityDbContext _db;

        public MfaHandler(TenantsIdentityDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MfaRequirement requirement)
        {
            if (context.User.HasClaim(c => (c.Type == "amr" && c.Value == "mfa") || (c.Type == "mfa" && c.Value == "true")))
            {
                context.Succeed(requirement);
                return;
            }

            var sub = context.User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
            {
                context.Fail();
                return;
            }

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null && user.MfaEnabled)
            {
                context.Fail();
                return;
            }

            context.Succeed(requirement);
        }
    }
}
