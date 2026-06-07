namespace Lms.Application.Abstractions;

/// <summary>
/// Hashes and verifies passwords. Implemented in Infrastructure (Argon2id). Hashes are stored in
/// PHC string format so parameters travel with the hash and can be upgraded on login.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password, returning a self-describing PHC string.</summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a stored PHC hash. <see cref="PasswordVerificationResult.NeedsRehash"/>
    /// is true when the stored parameters are weaker than the current policy (rehash-on-login).
    /// </summary>
    PasswordVerificationResult Verify(string hash, string password);
}

public readonly record struct PasswordVerificationResult(bool Succeeded, bool NeedsRehash);
