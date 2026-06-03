namespace Lms.Application.Abstractions;

/// <summary>
/// Generates opaque refresh tokens and hashes them for storage. The raw token is a high-entropy
/// random value handed to the client; only its hash is persisted. Implemented in Infrastructure
/// (SHA-256 — a fast hash is correct here, unlike passwords, because the token is high-entropy).
/// </summary>
public interface IRefreshTokenHasher
{
    /// <summary>Creates a new random token, returning the raw value (for the client) and its hash (to store).</summary>
    GeneratedRefreshToken Generate();

    /// <summary>Hashes a presented raw token so it can be matched against the stored hash.</summary>
    string Hash(string rawToken);
}

public readonly record struct GeneratedRefreshToken(string RawToken, string TokenHash);
