using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class MfaService : IMfaService
    {
        private readonly TenantsIdentityDbContext _db;

        public MfaService(TenantsIdentityDbContext db)
        {
            _db = db;
        }

        public Task<string> GenerateChallengeAsync(Guid userId)
        {
            // Placeholder challenge, to be replaced by TOTP/OTP provider.
            return Task.FromResult($"mfa-challenge-{userId:N}");
        }

        public Task<bool> VerifyCodeAsync(Guid userId, string code)
        {
            // Placeholder verification.
            return Task.FromResult(!string.IsNullOrWhiteSpace(code));
        }

        public async Task EnableAsync(Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return;
            }

            user.MfaEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task DisableAsync(Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return;
            }

            user.MfaEnabled = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
