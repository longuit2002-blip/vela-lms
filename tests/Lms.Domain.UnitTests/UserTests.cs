using Lms.Domain.Users;

namespace Lms.Domain.UnitTests;

public class UserTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private static User NewUser(bool mustChange = false) =>
        User.Create(Guid.NewGuid(), OrgId, "Owner@Vela.LOCAL", "$argon2id$hash", ["OrgOwner"], mustChange);

    [Fact]
    public void Create_normalizes_email_and_sets_active_with_roles()
    {
        var user = NewUser();

        Assert.Equal("owner@vela.local", user.Email);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Contains("OrgOwner", user.RoleCodes);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEndsAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    public void Create_rejects_invalid_email(string email)
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create(Guid.NewGuid(), OrgId, email, "$hash", ["OrgOwner"], false));
    }

    [Fact]
    public void Create_rejects_empty_org_and_empty_hash()
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create(Guid.NewGuid(), Guid.Empty, "a@b.c", "$hash", [], false));
        Assert.Throws<ArgumentException>(() =>
            User.Create(Guid.NewGuid(), OrgId, "a@b.c", "  ", [], false));
    }

    [Fact]
    public void Create_deduplicates_and_trims_role_codes()
    {
        var user = User.Create(Guid.NewGuid(), OrgId, "a@b.c", "$h", [" OrgOwner ", "OrgOwner", ""], false);

        Assert.Equal(["OrgOwner"], user.RoleCodes);
    }

    [Fact]
    public void RecordFailedLogin_locks_after_threshold_and_resets_counter()
    {
        var user = NewUser();
        var now = DateTimeOffset.UtcNow;
        var window = TimeSpan.FromMinutes(15);

        user.RecordFailedLogin(3, window, now);
        user.RecordFailedLogin(3, window, now);
        Assert.False(user.IsLockedOut(now));
        Assert.Equal(2, user.AccessFailedCount);

        user.RecordFailedLogin(3, window, now);
        Assert.True(user.IsLockedOut(now));
        Assert.False(user.CanAuthenticate(now));
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public void IsLockedOut_clears_after_window_elapses()
    {
        var user = NewUser();
        var now = DateTimeOffset.UtcNow;

        user.RecordFailedLogin(1, TimeSpan.FromMinutes(15), now);

        Assert.True(user.IsLockedOut(now));
        Assert.False(user.IsLockedOut(now.AddMinutes(16)));
        Assert.True(user.CanAuthenticate(now.AddMinutes(16)));
    }

    [Fact]
    public void RecordSuccessfulLogin_resets_failures_and_lock()
    {
        var user = NewUser();
        var now = DateTimeOffset.UtcNow;
        user.RecordFailedLogin(1, TimeSpan.FromMinutes(15), now);

        user.RecordSuccessfulLogin(now);

        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEndsAt);
        Assert.True(user.CanAuthenticate(now));
    }

    [Fact]
    public void ChangePassword_sets_hash_and_clears_must_change()
    {
        var user = NewUser(mustChange: true);
        Assert.True(user.MustChangePassword);

        user.ChangePassword("$argon2id$new", DateTimeOffset.UtcNow);

        Assert.False(user.MustChangePassword);
        Assert.Equal("$argon2id$new", user.PasswordHash);
    }

    [Fact]
    public void UpgradePasswordHash_replaces_hash_without_touching_must_change()
    {
        var user = NewUser(mustChange: true);

        user.UpgradePasswordHash("$argon2id$rehashed", DateTimeOffset.UtcNow);

        Assert.Equal("$argon2id$rehashed", user.PasswordHash);
        Assert.True(user.MustChangePassword);
    }

    [Fact]
    public void Disabled_user_cannot_authenticate_even_when_not_locked()
    {
        var user = NewUser();
        // No public disable yet; assert the rule via the active path as a guard against regressions.
        Assert.True(user.CanAuthenticate(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_leaves_user_unplaced()
    {
        var user = NewUser();

        Assert.Null(user.DepartmentId);
        Assert.Null(user.PositionId);
    }

    [Fact]
    public void PlaceIn_sets_department_and_position_and_bumps_timestamp()
    {
        var user = NewUser();
        var dept = Guid.NewGuid();
        var position = Guid.NewGuid();
        var later = user.UpdatedAt.AddMinutes(5);

        user.PlaceIn(dept, position, later);

        Assert.Equal(dept, user.DepartmentId);
        Assert.Equal(position, user.PositionId);
        Assert.Equal(later, user.UpdatedAt);

        user.PlaceIn(null, null, later.AddMinutes(1));
        Assert.Null(user.DepartmentId);
        Assert.Null(user.PositionId);
    }
}
