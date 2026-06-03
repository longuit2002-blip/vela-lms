using Lms.Domain.Organizations;

namespace Lms.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="Organization"/> aggregate. Focused (no generic repository)
/// — implemented in Infrastructure over EF Core.
/// </summary>
public interface IOrganizationRepository
{
    Task AddAsync(Organization organization, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task<Organization?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
