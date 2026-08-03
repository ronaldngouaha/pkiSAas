using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Tenants.Identity.Security
{
    public class ActiveUserTokenValidator
    {
        private readonly TenantsIdentityDbContext _db;

        public ActiveUserTokenValidator(TenantsIdentityDbContext db)
        {
            _db = db;
        }

        public async Task<bool> IsActiveAsync(ClaimsPrincipal? principal)
        {
            var subject = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("sub");

            if (!Guid.TryParse(subject, out var userId))
            {
                return false;
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == userId);

            return user?.IsActive == true;
        }
    }
}