using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class VaultKeyProvider : IKeyProvider
    {
        private readonly IConfiguration _configuration;
        private static readonly object FallbackLock = new();
        private static (string KeyId, RSAParameters PrivateKey)? _fallbackKey;

        public VaultKeyProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<(string KeyId, RSAParameters PrivateKey)> GetActiveRsaKeyAsync()
        {
            var fromVault = await TryGetFromVaultAsync();
            if (fromVault.HasValue)
            {
                return fromVault.Value;
            }

            return GetOrCreateLocalFallbackKey();
        }

        public async Task<string> GetPublicJwksAsync()
        {
            var fromVault = await TryGetPublicJwksFromVaultAsync();
            if (IsUsableJwks(fromVault))
            {
                return fromVault!;
            }

            var active = await GetActiveRsaKeyAsync();
            using var rsa = RSA.Create();
            rsa.ImportParameters(active.PrivateKey);
            var pub = rsa.ExportParameters(false);

            var jwk = new Dictionary<string, string>
            {
                ["kty"] = "RSA",
                ["use"] = "sig",
                ["alg"] = "RS256",
                ["kid"] = active.KeyId,
                ["n"] = WebEncoders.Base64UrlEncode(pub.Modulus ?? Array.Empty<byte>()),
                ["e"] = WebEncoders.Base64UrlEncode(pub.Exponent ?? Array.Empty<byte>())
            };

            var jwks = new Dictionary<string, object>
            {
                ["keys"] = new[] { jwk }
            };

            return JsonSerializer.Serialize(jwks);
        }

        private static bool IsUsableJwks(string? jwks)
        {
            if (string.IsNullOrWhiteSpace(jwks))
            {
                return false;
            }

            try
            {
                var signingKeys = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(jwks).GetSigningKeys();
                return signingKeys.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<(string KeyId, RSAParameters PrivateKey)?> TryGetFromVaultAsync()
        {
            try
            {
                var addr = Environment.GetEnvironmentVariable("VAULT_ADDR");
                var token = Environment.GetEnvironmentVariable("VAULT_TOKEN");
                if (string.IsNullOrWhiteSpace(addr) || string.IsNullOrWhiteSpace(token))
                {
                    return null;
                }

                var mountPoint = _configuration["Vault:MountPoint"] ?? "secret";
                var secretPath = _configuration["Vault:JwtKeyPath"] ?? "tenants-identity/jwt";

                var authMethod = new TokenAuthMethodInfo(token);
                var settings = new VaultClientSettings(addr, authMethod);
                var client = new VaultClient(settings);

                var secret = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: secretPath, mountPoint: mountPoint);
                var data = secret?.Data?.Data;
                if (data == null)
                {
                    return null;
                }

                if (!TryGetString(data, "privateKeyPem", out var privatePem) || string.IsNullOrWhiteSpace(privatePem))
                {
                    return null;
                }

                var keyId = TryGetString(data, "keyId", out var kid) && !string.IsNullOrWhiteSpace(kid)
                    ? kid
                    : ComputeKeyIdFromPem(privatePem);

                using var rsa = RSA.Create();
                rsa.ImportFromPem(privatePem);
                var p = rsa.ExportParameters(true);
                return (keyId, p);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> TryGetPublicJwksFromVaultAsync()
        {
            try
            {
                var addr = Environment.GetEnvironmentVariable("VAULT_ADDR");
                var token = Environment.GetEnvironmentVariable("VAULT_TOKEN");
                if (string.IsNullOrWhiteSpace(addr) || string.IsNullOrWhiteSpace(token))
                {
                    return null;
                }

                var mountPoint = _configuration["Vault:MountPoint"] ?? "secret";
                var secretPath = _configuration["Vault:JwtKeyPath"] ?? "tenants-identity/jwt";

                var authMethod = new TokenAuthMethodInfo(token);
                var settings = new VaultClientSettings(addr, authMethod);
                var client = new VaultClient(settings);

                var secret = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: secretPath, mountPoint: mountPoint);
                var data = secret?.Data?.Data;
                if (data == null)
                {
                    return null;
                }

                return TryGetString(data, "publicJwks", out var jwks) ? jwks : null;
            }
            catch
            {
                return null;
            }
        }

        private (string KeyId, RSAParameters PrivateKey) GetOrCreateLocalFallbackKey()
        {
            if (_fallbackKey.HasValue)
            {
                return _fallbackKey.Value;
            }

            lock (FallbackLock)
            {
                if (_fallbackKey.HasValue)
                {
                    return _fallbackKey.Value;
                }

                var pem = _configuration["Jwt:Signing:RsaPrivateKeyPem"];
                if (!string.IsNullOrWhiteSpace(pem))
                {
                    using var rsaFromPem = RSA.Create();
                    rsaFromPem.ImportFromPem(pem);
                    var keyIdFromPem = _configuration["Jwt:Signing:KeyId"] ?? ComputeKeyId(rsaFromPem.ExportParameters(false));
                    _fallbackKey = (keyIdFromPem, rsaFromPem.ExportParameters(true));
                    return _fallbackKey.Value;
                }

                var localPemPath = ResolveLocalFallbackPemPath();
                if (File.Exists(localPemPath))
                {
                    var existingPem = File.ReadAllText(localPemPath);
                    if (!string.IsNullOrWhiteSpace(existingPem))
                    {
                        using var rsaFromFile = RSA.Create();
                        rsaFromFile.ImportFromPem(existingPem);
                        var keyIdFromFile = _configuration["Jwt:Signing:KeyId"] ?? ComputeKeyId(rsaFromFile.ExportParameters(false));
                        _fallbackKey = (keyIdFromFile, rsaFromFile.ExportParameters(true));
                        return _fallbackKey.Value;
                    }
                }

                using var rsa = RSA.Create(2048);
                var privateParams = rsa.ExportParameters(true);
                var keyId = ComputeKeyId(rsa.ExportParameters(false));
                var generatedPem = rsa.ExportRSAPrivateKeyPem();

                var directory = Path.GetDirectoryName(localPemPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(localPemPath, generatedPem);
                _fallbackKey = (keyId, privateParams);
                return _fallbackKey.Value;
            }
        }

        private string ResolveLocalFallbackPemPath()
        {
            var configured = _configuration["Jwt:Signing:LocalFallbackPemPath"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Path.GetFullPath(configured);
            }

            // Keep a stable dev key under the content root so tokens survive restarts.
            var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            return Path.Combine(dataDir, "jwt-fallback-private.pem");
        }

        private static string ComputeKeyIdFromPem(string pem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return ComputeKeyId(rsa.ExportParameters(false));
        }

        private static string ComputeKeyId(RSAParameters pub)
        {
            var modulus = pub.Modulus ?? Array.Empty<byte>();
            var exponent = pub.Exponent ?? Array.Empty<byte>();
            var material = new byte[modulus.Length + exponent.Length];
            Buffer.BlockCopy(modulus, 0, material, 0, modulus.Length);
            Buffer.BlockCopy(exponent, 0, material, modulus.Length, exponent.Length);
            var hash = SHA256.HashData(material);
            return WebEncoders.Base64UrlEncode(hash);
        }

        private static bool TryGetString(IDictionary<string, object> data, string key, out string value)
        {
            value = string.Empty;
            if (!data.TryGetValue(key, out var obj) || obj == null)
            {
                return false;
            }

            value = obj.ToString() ?? string.Empty;
            return true;
        }
    }
}
