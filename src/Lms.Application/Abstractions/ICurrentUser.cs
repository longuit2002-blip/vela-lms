namespace Lms.Application.Abstractions;

/// <summary>
/// The current authenticated user, resolved from JWT claims. <see cref="UserId"/> is
/// <see cref="Guid.Empty"/> when unauthenticated.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>
    /// The caller's role codes, read from the <c>roles</c> claim. Effective permissions are resolved
    /// from these server-side (see <see cref="IPermissionResolver"/>); empty when unauthenticated.
    /// </summary>
    IReadOnlyCollection<string> RoleCodes { get; }
}
