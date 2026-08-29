using System.Security.Cryptography;
using EcomApi.Models;
using Microsoft.AspNetCore.DataProtection;
using OtpNet;

namespace EcomApi.Services;

/// <summary>
/// Two-factor authentication helpers: TOTP secret lifecycle, code verification,
/// and recovery-code generation. The shared secret is encrypted at rest with
/// ASP.NET Core Data Protection (never persisted in plaintext).
/// </summary>
public class TwoFactorService
{
    private const string Purpose = "Ecom.TwoFactor";
    private readonly IDataProtector _protector;

    public TwoFactorService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    /// <summary>Generates a new Base32 secret and the corresponding otpauth URI for QR codes.</summary>
    public (string PlainSecret, string OtpAuthUri) GenerateSetup(string email, string? issuer = "Ecom")
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var plainSecret = Base32Encoding.ToString(secretBytes);
        var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer ?? "Ecom")}:{Uri.EscapeDataString(email)}?secret={plainSecret}&issuer={Uri.EscapeDataString(issuer ?? "Ecom")}&digits=6";
        return (plainSecret, uri);
    }

    /// <summary>Encrypts a plaintext Base32 secret for storage.</summary>
    public string ProtectSecret(string plainSecret) => _protector.Protect(plainSecret);

    /// <summary>Verifies a 6-digit code against the stored (encrypted) secret.</summary>
    public bool VerifyCode(string? encryptedSecret, string code)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret) || string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            var plainSecret = _protector.Unprotect(encryptedSecret);
            var secretBytes = Base32Encoding.ToBytes(plainSecret);
            var totp = new Totp(secretBytes);
            return totp.VerifyTotp(code, out _, new VerificationWindow(2));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Generates recovery codes. Caller persists the hashes; the plaintext list is shown once.</summary>
    public List<(string Plain, string Hash)> GenerateRecoveryCodes(int count = 10)
    {
        var result = new List<(string, string)>(count);
        for (var i = 0; i < count; i++)
        {
            var plain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(9))
                .Replace("=", "", StringComparison.Ordinal)
                .Replace("+", "", StringComparison.Ordinal)
                .Replace("/", "", StringComparison.Ordinal);
            result.Add((plain, PasswordResetToken.HashToken(plain)));
        }
        return result;
    }

    /// <summary>Validates a recovery code against the user's unused, hashed codes. Returns true and marks it used.</summary>
    public bool TryConsumeRecoveryCode(User user, string code)
    {
        var normalized = code.Trim();
        var hash = PasswordResetToken.HashToken(normalized);
        var match = user.RecoveryCodes.FirstOrDefault(r => !r.IsUsed && r.CodeHash == hash);
        if (match == null)
            return false;

        match.IsUsed = true;
        match.UsedAt = DateTime.UtcNow;
        return true;
    }
}
