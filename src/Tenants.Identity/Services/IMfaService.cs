using System;
using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IMfaService
    {
        Task<string> GenerateChallengeAsync(Guid userId);
        Task<bool> VerifyCodeAsync(Guid userId, string code);
        Task EnableAsync(Guid userId);
        Task DisableAsync(Guid userId);
    }
}
