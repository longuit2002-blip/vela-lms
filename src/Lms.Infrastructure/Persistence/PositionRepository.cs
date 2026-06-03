using Lms.Application.Abstractions;
using Lms.Domain.Positions;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

public sealed class PositionRepository(AppDbContext db) : IPositionRepository
{
    public async Task AddAsync(Position position, CancellationToken cancellationToken)
        => await db.Positions.AddAsync(position, cancellationToken);

    public Task<Position?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Positions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Position>> ListByOrganizationAsync(CancellationToken cancellationToken)
        => await db.Positions.OrderBy(p => p.Name).ToListAsync(cancellationToken);

    // Explicit org scope + IgnoreQueryFilters so it is correct under the no-tenant seed path too.
    public Task<bool> NameExistsAsync(Guid organizationId, string name, CancellationToken cancellationToken)
        => db.Positions.IgnoreQueryFilters()
            .AnyAsync(p => p.OrganizationId == organizationId && p.Name == name, cancellationToken);

    public Task<bool> HasAssignedUsersAsync(Guid positionId, CancellationToken cancellationToken)
        => db.Users.AnyAsync(u => u.PositionId == positionId, cancellationToken);

    public Task RemoveAsync(Position position, CancellationToken cancellationToken)
    {
        db.Positions.Remove(position);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
