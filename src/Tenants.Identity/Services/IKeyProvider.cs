using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IKeyProvider
    {
        Task<(string KeyId, RSAParameters PrivateKey)> GetActiveRsaKeyAsync();
        Task<string> GetPublicJwksAsync(); // optional: expose JWKS for other services
    }
}