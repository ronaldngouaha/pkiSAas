using System;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IAuthService
    {
        Task<AuthResultDto> LoginAsync(LoginRequestDto dto, string ip);
        Task<AuthResultDto> RefreshAsync(string refreshToken, string ip);
        Task RevokeRefreshTokenAsync(string refreshToken, string ip);
        Task<UserDto> RegisterAsync(Guid? tenantId, RegisterRequestDto dto);
        Task SeedSuperAdminAsync(RegisterRequestDto dto);
        Task<bool> ValidatePasswordAsync(string email, string password);
    }
}