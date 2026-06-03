namespace Lms.Domain.Users;

/// <summary>
/// Account status. Temporary brute-force lockout is represented separately by a time-based
/// lockout timestamp (see <see cref="User"/>), not by this enum — so <c>Active</c> vs
/// <c>Disabled</c> here is the durable administrative state.
/// </summary>
public enum UserStatus
{
    Active,
    Disabled,
}
