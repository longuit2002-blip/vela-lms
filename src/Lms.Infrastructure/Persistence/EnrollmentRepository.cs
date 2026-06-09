using Lms.Application.Abstractions;
using Lms.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

/// <summary>
/// Enrollment persistence. Lookups honor the tenant query filter; the caller additionally scopes by
/// user id so a foreign enrollment reads as not-found. Lesson-progress rows load with the enrollment.
/// </summary>
public sealed class EnrollmentRepository(AppDbContext db) : IEnrollmentRepository
{
    public async Task AddRangeAsync(IEnumerable<Enrollment> enrollments, CancellationToken cancellationToken)
        => await db.Enrollments.AddRangeAsync(enrollments, cancellationToken);

    public Task<Enrollment?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Enrollments
            .Include(e => e.LessonProgress)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
        => await db.Enrollments
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid userId, Guid publicationId, CancellationToken cancellationToken)
        => db.Enrollments.AnyAsync(e => e.UserId == userId && e.PublicationId == publicationId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
