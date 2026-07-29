using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs.Mfa;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IMfaService
    {
        Task<MfaSetupDto> BeginTotpSetupAsync(Guid userId);
        Task<bool> VerifyTotpAsync(Guid userId, string code);
        Task<string[]> GenerateRecoveryCodesAsync(Guid userId, int count = 10);
        Task<bool> ConsumeRecoveryCodeAsync(Guid userId, string code);
        Task DisableTotpAsync(Guid userId);
        Task<bool> IsMfaEnabledAsync(Guid userId);
    }
}
