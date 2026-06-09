using Lms.Domain.Courses;

namespace Lms.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="Course"/> aggregate (with its modules and lessons). Lookups
/// honor the tenant query filter — never bypassed (IDOR control). Modules/lessons carry no tenant
/// column; they are isolated transitively because they are only loaded through their course.
/// </summary>
public interface ICourseRepository
{
    Task AddAsync(Course course, CancellationToken cancellationToken);

    /// <summary>Tenant-scoped lookup by id, with modules and lessons loaded.</summary>
    Task<Course?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tenant-scoped lookup by id, with modules and lessons loaded — for resolving read models.</summary>
    Task<IReadOnlyList<Course>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
