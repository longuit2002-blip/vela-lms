using Lms.Domain.Positions;

namespace Lms.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="Position"/> aggregate (flat org-wide catalog). Focused
/// (no generic repository), implemented in Infrastructure over EF Core.
/// </summary>
public interface IPositionRepository
{
    Task AddAsync(Position position, CancellationToken cancellationToken);

    Task<Position?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Position>> ListByOrganizationAsync(CancellationToken cancellationToken);

    /// <summary>True if a position with this name already exists in the organization (case-sensitive on the stored name).</summary>
    Task<bool> NameExistsAsync(Guid organizationId, string name, CancellationToken cancellationToken);

    /// <summary>True if at least one user holds the position.</summary>
    Task<bool> HasAssignedUsersAsync(Guid positionId, CancellationToken cancellationToken);

    Task RemoveAsync(Position position, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
