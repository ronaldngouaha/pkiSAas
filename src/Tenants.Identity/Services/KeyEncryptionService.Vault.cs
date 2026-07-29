using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class KeyEncryptionService : IKeyEncryptionService
    {
        // Dev placeholder: use VaultSharp or Azure Key Vault in prod
        public Task<(string Encrypted, string KeyId)> EncryptAsync(string plaintext)
        {
            // In dev: return base64 of plaintext as "encrypted" and keyId = "dev-key"
            var enc = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));
            return Task.FromResult((enc, "dev-key"));
        }

        public Task<string> DecryptAsync(string encrypted, string keyId)
        {
            var bytes = System.Convert.FromBase64String(encrypted);
            var plain = System.Text.Encoding.UTF8.GetString(bytes);
            return Task.FromResult(plain);
        }
    }
}
