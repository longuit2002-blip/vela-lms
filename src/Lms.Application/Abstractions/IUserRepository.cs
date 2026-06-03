using Lms.Domain.Users;

namespace Lms.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="User"/> aggregate. Focused (no generic repository),
/// implemented in Infrastructure over EF Core.
/// </summary>
public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Looks up a user by email for the login path. This runs <b>before</b> tenant context exists,
    /// so the implementation bypasses the tenant query filter; under RLS it must run via the
    /// maintenance (BYPASSRLS) path (see U5). Email is matched case-insensitively (stored normalized).
    /// </summary>
    Task<User?> FindByEmailForLoginAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(Guid organizationId, string email, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
