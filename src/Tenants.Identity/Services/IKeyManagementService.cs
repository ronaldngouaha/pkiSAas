using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IKeyManagementService
    {
        Task<(string KeyId, RSAParameters PrivateKey)> GetActiveSigningKeyAsync();
        Task<string> GetPublicJwksAsync();
    }
}
