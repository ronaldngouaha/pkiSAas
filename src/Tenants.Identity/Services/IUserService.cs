using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IUserService
    {
        Task<UserDto> CreateAsync(Guid tenantId, UserCreateDto dto);
        Task<UserDto> GetAsync(Guid tenantId, Guid userId);
        Task<UserDto> UpdateAsync(Guid tenantId, Guid userId, UserUpdateDto dto);
        Task<UserDto> AddRoleAsync(Guid tenantId, Guid userId, string role);
        Task<IEnumerable<UserDto>> ListAsync(Guid tenantId, int page = 1, int pageSize = 50);
        Task DeactivateAsync(Guid tenantId, Guid userId, string reason);
        Task ReactivateAsync(Guid tenantId, Guid userId, string reason);
        Task ChangePasswordAsync(Guid tenantId, Guid userId, string newPassword);
    }
}
