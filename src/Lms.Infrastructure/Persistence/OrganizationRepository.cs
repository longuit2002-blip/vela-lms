using Lms.Application.Abstractions;
using Lms.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

public sealed class OrganizationRepository(AppDbContext db) : IOrganizationRepository
{
    public async Task AddAsync(Organization organization, CancellationToken cancellationToken)
        => await db.Organizations.AddAsync(organization, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
        => db.Organizations.AnyAsync(o => o.Slug == slug, cancellationToken);

    public Task<Organization?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Organization?> FindBySlugAsync(string slug, CancellationToken cancellationToken)
        => db.Organizations.FirstOrDefaultAsync(o => o.Slug == slug, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
