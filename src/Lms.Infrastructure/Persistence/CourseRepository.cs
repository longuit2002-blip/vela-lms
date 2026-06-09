using Lms.Application.Abstractions;
using Lms.Domain.Courses;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

/// <summary>
/// Course persistence. Lookups honor the tenant query filter (never bypassed — IDOR control) and
/// load modules + lessons so the publish gate and course detail can read them.
/// </summary>
public sealed class CourseRepository(AppDbContext db) : ICourseRepository
{
    public async Task AddAsync(Course course, CancellationToken cancellationToken)
        => await db.Courses.AddAsync(course, cancellationToken);

    public Task<Course?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Courses
            .Include(c => c.Modules).ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Course>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        => await db.Courses
            .Include(c => c.Modules).ThenInclude(m => m.Lessons)
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

    // The aggregate adds the module/lesson to its own collection, but EF marks a graph-discovered child
    // with a client-set key as Modified (it assumes the row exists). Explicitly staging it as Added is
    // what makes SaveChanges INSERT rather than UPDATE-a-missing-row.
    public void AddModule(Module module) => db.Add(module);

    public void AddLesson(Lesson lesson) => db.Add(lesson);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
