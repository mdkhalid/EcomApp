using System.Security.Cryptography;
using System.Text;

namespace EcomApi.Services;

/// <summary>
/// Pure helpers for password-reset tokens: generation, hashing, and validation.
/// Kept side-effect free so it is trivially unit-testable.
/// </summary>
public static class PasswordResetToken
{
    public static string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool IsValid(string? token, string? storedHash, DateTime? expiry)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(storedHash) || expiry == null)
            return false;
        if (expiry < DateTime.UtcNow)
            return false;
        return string.Equals(HashToken(token), storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
