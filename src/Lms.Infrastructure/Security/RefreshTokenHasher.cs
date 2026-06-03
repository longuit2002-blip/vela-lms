using System.Security.Cryptography;
using System.Text;
using Lms.Application.Abstractions;

namespace Lms.Infrastructure.Security;

/// <summary>
/// Generates 256-bit opaque refresh tokens (base64url) and hashes them with SHA-256 (hex) for
/// storage. A fast hash is correct here — the token is high-entropy, not a low-entropy password.
/// </summary>
public sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    private const int TokenBytes = 32;

    public GeneratedRefreshToken Generate()
    {
        var raw = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        return new GeneratedRefreshToken(raw, Hash(raw));
    }

    public string Hash(string rawToken)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken ?? string.Empty));
        return Convert.ToHexString(digest); // 64 hex chars
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
