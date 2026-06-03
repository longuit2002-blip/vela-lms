using Lms.Domain.SeedWork;

namespace Lms.Domain.Users;

/// <summary>
/// User aggregate root — an authenticatable identity that belongs to exactly one organization
/// (the tenant). Minimal for the Identity + Auth slice: no department/position yet. Roles ride as
/// claim codes only (no permission enforcement this slice). Brute-force lockout is time-based.
/// </summary>
public sealed class User : Entity, IAggregateRoot
{
    private readonly List<string> _roleCodes = [];

    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserStatus Status { get; private set; }
    public bool MustChangePassword { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTimeOffset? LockoutEndsAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<string> RoleCodes => _roleCodes.AsReadOnly();

    // Required by EF Core for materialization.
    private User() { }

    private User(
        Guid id,
        Guid organizationId,
        string email,
        string passwordHash,
        IEnumerable<string> roleCodes,
        bool mustChangePassword,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        Email = email;
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        MustChangePassword = mustChangePassword;
        _roleCodes.AddRange(roleCodes);
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a new active user. <paramref name="id"/> is supplied by the Application layer
    /// (UUID v7) so the Domain stays free of the generation strategy.
    /// </summary>
    public static User Create(
        Guid id,
        Guid organizationId,
        string email,
        string passwordHash,
        IReadOnlyCollection<string> roleCodes,
        bool mustChangePassword)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));

        email = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));

        ArgumentNullException.ThrowIfNull(roleCodes);

        var normalizedRoles = roleCodes
            .Select(r => (r ?? string.Empty).Trim())
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new User(id, organizationId, email, passwordHash, normalizedRoles, mustChangePassword, DateTimeOffset.UtcNow);
    }

    /// <summary>Whether the account is currently locked out due to failed logins, at <paramref name="now"/>.</summary>
    public bool IsLockedOut(DateTimeOffset now) => LockoutEndsAt is { } until && until > now;

    /// <summary>Whether the account may attempt to authenticate at <paramref name="now"/> (active and not locked).</summary>
    public bool CanAuthenticate(DateTimeOffset now) => Status == UserStatus.Active && !IsLockedOut(now);

    /// <summary>
    /// Records a failed login attempt. On reaching <paramref name="maxFailedAttempts"/> the account is
    /// locked for <paramref name="lockoutWindow"/> and the counter resets so a post-expiry attempt starts fresh.
    /// </summary>
    public void RecordFailedLogin(int maxFailedAttempts, TimeSpan lockoutWindow, DateTimeOffset now)
    {
        if (maxFailedAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxFailedAttempts));

        AccessFailedCount++;
        if (AccessFailedCount >= maxFailedAttempts)
        {
            LockoutEndsAt = now + lockoutWindow;
            AccessFailedCount = 0;
        }

        UpdatedAt = now;
    }

    /// <summary>Resets failed-attempt state after a successful authentication.</summary>
    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        AccessFailedCount = 0;
        LockoutEndsAt = null;
        UpdatedAt = now;
    }

    /// <summary>Sets a new password hash and clears the forced-change flag.</summary>
    public void ChangePassword(string newPasswordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        UpdatedAt = now;
    }

    /// <summary>Replaces the stored hash without touching the forced-change flag (rehash-on-login).</summary>
    public void UpgradePasswordHash(string newPasswordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        UpdatedAt = now;
    }

    private static string NormalizeEmail(string? email)
    {
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length == 0)
            throw new ArgumentException("Email is required.", nameof(email));
        if (!email.Contains('@'))
            throw new ArgumentException("Email must contain '@'.", nameof(email));
        return email;
    }
}
