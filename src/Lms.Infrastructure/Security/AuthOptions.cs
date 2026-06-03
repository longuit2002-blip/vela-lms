using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Lms.Infrastructure.Security;

/// <summary>
/// JWT issuance + validation settings (config section <c>Jwt</c>). For dev, leave
/// <see cref="PrivateKeyPem"/> empty to generate an ephemeral RSA key on startup; production
/// supplies a PEM (from a secret store, never committed).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "lms";

    [Required]
    public string Audience { get; init; } = "lms";

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 14;

    /// <summary>Optional PEM-encoded RSA private key. Empty in dev → an ephemeral key is generated.</summary>
    public string? PrivateKeyPem { get; init; }

    /// <summary>Key id stamped on issued tokens (JWKS/rotation use later).</summary>
    public string KeyId { get; init; } = "dev";
}

/// <summary>Brute-force lockout policy (config section <c>Lockout</c>). Per-account only this slice.</summary>
public sealed class LockoutOptions
{
    public const string SectionName = "Lockout";

    [Range(1, 100)]
    public int MaxFailedAttempts { get; init; } = 5;

    [Range(1, 1440)]
    public int LockoutMinutes { get; init; } = 15;
}

/// <summary>Coarse interim rate limit on login (config section <c>RateLimit</c>). No Redis.</summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    [Range(1, 1000)]
    public int LoginAttemptsPerMinute { get; init; } = 10;
}

/// <summary>
/// Dev seeder settings (config section <c>Seed</c>). Provisions the first org + OrgOwner.
/// <see cref="Enabled"/> guards it off outside development.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; init; }

    public string OrganizationName { get; init; } = "";

    public string OrganizationSlug { get; init; } = "";

    public string OwnerEmail { get; init; } = "";

    public string OwnerPassword { get; init; } = "";
}

/// <summary>
/// Validates seed settings only when seeding is enabled — a disabled seeder needs no credentials,
/// but an enabled one must carry a sufficiently strong default password (forced-change on first login).
/// </summary>
public sealed class SeedOptionsValidator : IValidateOptions<SeedOptions>
{
    private const int MinPasswordLength = 12;

    public ValidateOptionsResult Validate(string? name, SeedOptions options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.OrganizationName))
            failures.Add("Seed.OrganizationName is required when seeding is enabled.");
        if (string.IsNullOrWhiteSpace(options.OrganizationSlug))
            failures.Add("Seed.OrganizationSlug is required when seeding is enabled.");
        if (string.IsNullOrWhiteSpace(options.OwnerEmail))
            failures.Add("Seed.OwnerEmail is required when seeding is enabled.");
        if ((options.OwnerPassword ?? "").Length < MinPasswordLength)
            failures.Add($"Seed.OwnerPassword must be at least {MinPasswordLength} characters when seeding is enabled.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
