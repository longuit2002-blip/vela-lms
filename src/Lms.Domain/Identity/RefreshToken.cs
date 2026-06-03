using Lms.Domain.SeedWork;

namespace Lms.Domain.Identity;

/// <summary>
/// A single refresh token in a rotating family. Only the SHA-256 <see cref="TokenHash"/> of the
/// opaque token is stored (never the raw value). Rotation links tokens via <see cref="ParentId"/> /
/// <see cref="ReplacedById"/> sharing one <see cref="FamilyId"/>; reuse of a consumed token revokes
/// the whole family. Tenant-owned (carries <see cref="OrganizationId"/>).
/// </summary>
public sealed class RefreshToken : Entity, IAggregateRoot
{
    public Guid FamilyId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public Guid? ReplacedById { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    // Required by EF Core for materialization.
    private RefreshToken() { }

    private RefreshToken(
        Guid id,
        Guid familyId,
        Guid userId,
        Guid organizationId,
        string tokenHash,
        Guid? parentId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        FamilyId = familyId;
        UserId = userId;
        OrganizationId = organizationId;
        TokenHash = tokenHash;
        ParentId = parentId;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>Creates the first token of a new family (its <see cref="FamilyId"/> equals its own id).</summary>
    public static RefreshToken CreateRoot(
        Guid id,
        Guid userId,
        Guid organizationId,
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        Validate(id, userId, organizationId, tokenHash, issuedAt, expiresAt);
        return new RefreshToken(id, id, userId, organizationId, tokenHash, parentId: null, issuedAt, expiresAt);
    }

    /// <summary>Creates a child token that continues <paramref name="parent"/>'s family on rotation.</summary>
    public static RefreshToken CreateChild(
        Guid id,
        RefreshToken parent,
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(parent);
        Validate(id, parent.UserId, parent.OrganizationId, tokenHash, issuedAt, expiresAt);
        return new RefreshToken(id, parent.FamilyId, parent.UserId, parent.OrganizationId, tokenHash, parent.Id, issuedAt, expiresAt);
    }

    /// <summary>Active = not yet used, not revoked, and not expired at <paramref name="now"/>.</summary>
    public bool IsActive(DateTimeOffset now) => UsedAt is null && RevokedAt is null && ExpiresAt > now;

    /// <summary>Marks this token consumed by a rotation that minted <paramref name="childId"/>.</summary>
    public void MarkRotated(Guid childId, DateTimeOffset now)
    {
        if (childId == Guid.Empty)
            throw new ArgumentException("ChildId is required.", nameof(childId));

        UsedAt = now;
        RevokedAt = now;
        RevokedReason = "rotated";
        ReplacedById = childId;
    }

    /// <summary>Revokes the token with a reason. Idempotent — a no-op if already revoked.</summary>
    public void Revoke(string reason, DateTimeOffset now)
    {
        if (RevokedAt is not null)
            return;

        RevokedAt = now;
        RevokedReason = reason;
    }

    private static void Validate(
        Guid id,
        Guid userId,
        Guid organizationId,
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("TokenHash is required.", nameof(tokenHash));
        if (expiresAt <= issuedAt)
            throw new ArgumentException("ExpiresAt must be after IssuedAt.", nameof(expiresAt));
    }
}
