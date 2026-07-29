using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Acme.Pki.Tenants.Identity.DTOs.Mfa;
using Acme.Pki.Tenants.Identity.Models;
using Acme.Pki.Tenants.Identity.Data;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;

namespace Acme.Pki.Tenants.Identity.Services
{
    public class MfaService : IMfaService
    {
        private readonly TenantsIdentityDbContext _db;
        private readonly IKeyEncryptionService _keyEncryption;
        private readonly string _issuer = "Acme.Pki";

        public MfaService(TenantsIdentityDbContext db, IKeyEncryptionService keyEncryption)
        {
            _db = db;
            _keyEncryption = keyEncryption;
        }

        public async Task<MfaSetupDto> BeginTotpSetupAsync(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            var secretBytes = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secretBytes);

            var label = Uri.EscapeDataString($"{_issuer}:{user.Email}");
            var issuerEscaped = Uri.EscapeDataString(_issuer);
            var otpauth = $"otpauth://totp/{label}?secret={base32Secret}&issuer={issuerEscaped}&algorithm=SHA1&digits=6&period=30";

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(otpauth, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);
            var qrBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";

            var (encrypted, keyId) = await _keyEncryption.EncryptAsync(base32Secret);

            var secretEntity = new UserMfaSecret
            {
                UserId = userId,
                EncryptedSecret = encrypted,
                KeyId = keyId,
                IsActive = true
            };

            _db.UserMfaSecrets.Add(secretEntity);
            await _db.SaveChangesAsync();

            return new MfaSetupDto
            {
                QrCodeBase64Png = qrBase64,
                ManualEntryKey = base32Secret
            };
        }

        public async Task<bool> VerifyTotpAsync(Guid userId, string code)
        {
            var secretEntity = await _db.UserMfaSecrets
                .Where(s => s.UserId == userId && s.IsActive && s.RevokedAt == null)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (secretEntity == null)
            {
                return false;
            }

            var base32Secret = await _keyEncryption.DecryptAsync(secretEntity.EncryptedSecret, secretEntity.KeyId);
            if (string.IsNullOrWhiteSpace(base32Secret))
            {
                return false;
            }

            var secretBytes = Base32Encoding.ToBytes(base32Secret);
            var totp = new Totp(secretBytes, step: 30, totpSize: 6);
            var window = new VerificationWindow(previous: 1, future: 1);
            var valid = totp.VerifyTotp(code, out _, window);

            if (valid)
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.MfaEnabled = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return valid;
        }

        public async Task<string[]> GenerateRecoveryCodesAsync(Guid userId, int count = 10)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            var codes = new List<string>();
            var hashed = new List<RecoveryCode>();

            for (var i = 0; i < count; i++)
            {
                var code = GenerateRecoveryCode();
                codes.Add(code);
                hashed.Add(new RecoveryCode
                {
                    UserId = userId,
                    CodeHash = HashCode(code)
                });
            }

            _db.RecoveryCodes.AddRange(hashed);
            await _db.SaveChangesAsync();
            return codes.ToArray();
        }

        public async Task<bool> ConsumeRecoveryCodeAsync(Guid userId, string code)
        {
            var hash = HashCode(code);
            var recoveryCode = await _db.RecoveryCodes
                .FirstOrDefaultAsync(r => r.UserId == userId && !r.Used && r.CodeHash == hash);

            if (recoveryCode == null)
            {
                return false;
            }

            recoveryCode.Used = true;
            recoveryCode.UsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task DisableTotpAsync(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException();
            }

            user.MfaEnabled = false;

            var secrets = await _db.UserMfaSecrets
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var secret in secrets)
            {
                secret.IsActive = false;
                secret.RevokedAt = DateTime.UtcNow;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task<bool> IsMfaEnabledAsync(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            return user?.MfaEnabled ?? false;
        }

        private static string GenerateRecoveryCode()
        {
            var bytes = RandomNumberGenerator.GetBytes(6);
            return BitConverter.ToString(bytes).Replace("-", string.Empty, StringComparison.Ordinal).Substring(0, 8);
        }

        private static string HashCode(string code)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToBase64String(hash);
        }
    }
}
