using Lms.Application.Abstractions;
using Lms.Domain.Publishing;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

/// <summary>Publication persistence. Lookups honor the tenant query filter (never bypassed).</summary>
public sealed class PublicationRepository(AppDbContext db) : IPublicationRepository
{
    public async Task AddAsync(Publication publication, CancellationToken cancellationToken)
        => await db.Publications.AddAsync(publication, cancellationToken);

    public Task<Publication?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Publications.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Publication>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        => await db.Publications.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
