using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class KeyManagementService : IKeyManagementService
    {
        private readonly IKeyProvider _keyProvider;
        private readonly IIdentityTelemetry _telemetry;
        private readonly ILogger<KeyManagementService> _logger;

        public KeyManagementService(IKeyProvider keyProvider, IIdentityTelemetry telemetry, ILogger<KeyManagementService> logger)
        {
            _keyProvider = keyProvider;
            _telemetry = telemetry;
            _logger = logger;
        }

        public async Task<(string KeyId, RSAParameters PrivateKey)> GetActiveSigningKeyAsync()
        {
            try
            {
                return await _keyProvider.GetActiveRsaKeyAsync();
            }
            catch (System.Exception ex)
            {
                _telemetry.RecordKeyRotationFailure("active_signing_key_resolution_failed");
                _logger.LogError(ex, "auth.key.rotation.failed reason={Reason}", "active_signing_key_resolution_failed");
                throw;
            }
        }

        public async Task<string> GetPublicJwksAsync()
        {
            try
            {
                return await _keyProvider.GetPublicJwksAsync();
            }
            catch (System.Exception ex)
            {
                _telemetry.RecordKeyRotationFailure("public_jwks_resolution_failed");
                _logger.LogError(ex, "auth.key.rotation.failed reason={Reason}", "public_jwks_resolution_failed");
                throw;
            }
        }
    }
}
