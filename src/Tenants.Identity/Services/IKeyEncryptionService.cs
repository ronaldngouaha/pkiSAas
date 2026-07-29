using System.Threading.Tasks;

namespace Acme.Pki.Tenants.Identity.Services
{
    public interface IKeyEncryptionService
    {
        // Encrypt plaintext secret, return (ciphertext, keyId)
        Task<(string Encrypted, string KeyId)> EncryptAsync(string plaintext);

        // Decrypt ciphertext using keyId
        Task<string> DecryptAsync(string encrypted, string keyId);
    }
}
