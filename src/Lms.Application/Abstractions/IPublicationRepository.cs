using Lms.Domain.Publishing;

namespace Lms.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="Publication"/> aggregate. Lookups honor the tenant query
/// filter (never bypassed — IDOR control).
/// </summary>
public interface IPublicationRepository
{
    Task AddAsync(Publication publication, CancellationToken cancellationToken);

    /// <summary>Tenant-scoped lookup by id.</summary>
    Task<Publication?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tenant-scoped lookup by ids — for resolving read models.</summary>
    Task<IReadOnlyList<Publication>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
