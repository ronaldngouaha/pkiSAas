using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class KeyManagementService : IKeyManagementService
    {
        private readonly IKeyProvider _keyProvider;

        public KeyManagementService(IKeyProvider keyProvider)
        {
            _keyProvider = keyProvider;
        }

        public Task<(string KeyId, RSAParameters PrivateKey)> GetActiveSigningKeyAsync()
        {
            return _keyProvider.GetActiveRsaKeyAsync();
        }

        public Task<string> GetPublicJwksAsync()
        {
            return _keyProvider.GetPublicJwksAsync();
        }
    }
}
