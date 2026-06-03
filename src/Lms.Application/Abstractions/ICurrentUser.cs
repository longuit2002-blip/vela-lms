namespace Lms.Application.Abstractions;

/// <summary>
/// The current authenticated user, resolved from JWT claims. <see cref="UserId"/> is
/// <see cref="Guid.Empty"/> when unauthenticated.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    bool IsAuthenticated { get; }
}
