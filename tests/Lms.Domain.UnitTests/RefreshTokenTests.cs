using Lms.Domain.Identity;

namespace Lms.Domain.UnitTests;

public class RefreshTokenTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OrgId = Guid.NewGuid();

    private static RefreshToken NewRoot(DateTimeOffset now) =>
        RefreshToken.CreateRoot(Guid.NewGuid(), UserId, OrgId, "hash-1", now, now.AddDays(14));

    [Fact]
    public void CreateRoot_sets_family_to_own_id_and_no_parent()
    {
        var now = DateTimeOffset.UtcNow;
        var root = NewRoot(now);

        Assert.Equal(root.Id, root.FamilyId);
        Assert.Null(root.ParentId);
        Assert.True(root.IsActive(now));
    }

    [Fact]
    public void CreateChild_continues_family_and_links_parent()
    {
        var now = DateTimeOffset.UtcNow;
        var root = NewRoot(now);

        var child = RefreshToken.CreateChild(Guid.NewGuid(), root, "hash-2", now, now.AddDays(14));

        Assert.Equal(root.FamilyId, child.FamilyId);
        Assert.Equal(root.Id, child.ParentId);
        Assert.Equal(root.UserId, child.UserId);
        Assert.Equal(root.OrganizationId, child.OrganizationId);
    }

    [Fact]
    public void IsActive_false_when_expired_used_or_revoked()
    {
        var now = DateTimeOffset.UtcNow;

        var expired = RefreshToken.CreateRoot(Guid.NewGuid(), UserId, OrgId, "h", now.AddDays(-15), now.AddSeconds(-1));
        Assert.False(expired.IsActive(now));

        var rotated = NewRoot(now);
        rotated.MarkRotated(Guid.NewGuid(), now);
        Assert.False(rotated.IsActive(now));

        var revoked = NewRoot(now);
        revoked.Revoke("logout", now);
        Assert.False(revoked.IsActive(now));
    }

    [Fact]
    public void MarkRotated_sets_used_revoked_and_replaced_by()
    {
        var now = DateTimeOffset.UtcNow;
        var root = NewRoot(now);
        var childId = Guid.NewGuid();

        root.MarkRotated(childId, now);

        Assert.Equal(now, root.UsedAt);
        Assert.Equal(now, root.RevokedAt);
        Assert.Equal("rotated", root.RevokedReason);
        Assert.Equal(childId, root.ReplacedById);
    }

    [Fact]
    public void Revoke_is_idempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var root = NewRoot(now);

        root.Revoke("reuse_detected", now);
        root.Revoke("logout", now.AddMinutes(5));

        Assert.Equal(now, root.RevokedAt);
        Assert.Equal("reuse_detected", root.RevokedReason);
    }

    [Fact]
    public void CreateRoot_rejects_expiry_before_issue_and_empty_hash()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() =>
            RefreshToken.CreateRoot(Guid.NewGuid(), UserId, OrgId, "h", now, now));
        Assert.Throws<ArgumentException>(() =>
            RefreshToken.CreateRoot(Guid.NewGuid(), UserId, OrgId, " ", now, now.AddDays(1)));
    }
}
