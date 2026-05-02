using System.Security.Cryptography;

namespace LegalDocSystem.Infrastructure.Services;

/// <summary>
/// Shared helpers for generating and hashing security tokens (verification,
/// password reset, etc.). Both AuthService and UserService use these.
/// </summary>
internal static class TokenHelper
{
    /// <summary>
    /// Generates a cryptographically random, URL-safe base64 token (32 bytes).
    /// </summary>
    public static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').Replace("=", ""); // URL-safe
    }

    /// <summary>
    /// SHA-256 hashes a token for safe storage. Compare against the hash; never
    /// store the raw token.
    /// </summary>
    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
