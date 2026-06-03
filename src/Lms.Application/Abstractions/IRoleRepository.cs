using Lms.Domain.Roles;

namespace Lms.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="Role"/> catalog. This slice only deals with system roles
/// (org-wide, seeded once). Used by the seeder to provision them idempotently and by the permission
/// resolver to map a caller's role codes to their permission sets.
/// </summary>
public interface IRoleRepository
{
    Task AddAsync(Role role, CancellationToken cancellationToken);

    /// <summary>True if a system role with this code already exists (idempotent seeding).</summary>
    Task<bool> SystemRoleExistsAsync(string code, CancellationToken cancellationToken);

    /// <summary>All system roles (org-wide catalog), for the permission resolver to cache.</summary>
    Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
