using Lms.Domain.Learning;

namespace Lms.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="Enrollment"/> aggregate (with its lesson-progress rows).
/// Lookups honor the tenant query filter; learner-facing reads/writes additionally scope by the
/// caller's user id in the handler (a foreign enrollment must read as 404, not 403).
/// </summary>
public interface IEnrollmentRepository
{
    /// <summary>Stages one or more new enrollments (assign creates one per target learner).</summary>
    Task AddRangeAsync(IEnumerable<Enrollment> enrollments, CancellationToken cancellationToken);

    /// <summary>Tenant-scoped lookup by id, with lesson-progress rows loaded.</summary>
    Task<Enrollment?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tenant-scoped enrollments for one learner (newest first), without progress rows.</summary>
    Task<IReadOnlyList<Enrollment>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>True if the learner already has an enrollment for the publication (idempotent assign).</summary>
    Task<bool> ExistsAsync(Guid userId, Guid publicationId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
