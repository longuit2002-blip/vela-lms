using Lms.Domain.SeedWork;

namespace Lms.Domain.Learning;

/// <summary>
/// Enrollment aggregate root — a learner's assignment to a <see cref="Lms.Domain.Publishing.Publication"/>
/// and the consistency boundary for that learner's per-lesson progress. Owns its
/// <see cref="LessonProgress"/> rows; progress and completion are recomputed in-aggregate as lessons
/// are completed. Completion is synchronous (no domain events in this slice).
/// </summary>
public sealed class Enrollment : Entity, IAggregateRoot
{
    private readonly List<LessonProgress> _lessonProgress = new();

    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PublicationId { get; private set; }
    public EnrollmentSource Source { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public int ProgressPercent { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<LessonProgress> LessonProgress => _lessonProgress.AsReadOnly();

    // Required by EF Core for materialization.
    private Enrollment() { }

    private Enrollment(Guid id, Guid organizationId, Guid userId, Guid publicationId, EnrollmentSource source, DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        PublicationId = publicationId;
        Source = source;
        Status = EnrollmentStatus.NotStarted;
        ProgressPercent = 0;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates an Assigned enrollment seeded with a NotStarted <see cref="LessonProgress"/> per lesson.
    /// Rejects an empty lesson set — the "course has at least one lesson" rule is also guarded here,
    /// not only in the publish handler, so a zero-lesson enrollment can never exist.
    /// </summary>
    public static Enrollment CreateAssigned(
        Guid id, Guid organizationId, Guid userId, Guid publicationId, IEnumerable<Guid> lessonIds, DateTimeOffset now)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (publicationId == Guid.Empty)
            throw new ArgumentException("PublicationId is required.", nameof(publicationId));

        var ids = lessonIds?.ToList() ?? new List<Guid>();
        if (ids.Count == 0)
            throw new ArgumentException("An enrollment requires at least one lesson.", nameof(lessonIds));

        var enrollment = new Enrollment(id, organizationId, userId, publicationId, EnrollmentSource.Assigned, now);
        foreach (var lessonId in ids)
            enrollment._lessonProgress.Add(new LessonProgress(Guid.NewGuid(), id, lessonId));
        return enrollment;
    }

    /// <summary>
    /// Marks a lesson complete and recomputes progress. Idempotent if the lesson is already complete.
    /// Throws if the lesson is not part of this enrollment (the membership check is the security
    /// boundary — callers must not look the lesson up elsewhere).
    /// </summary>
    public void CompleteLesson(Guid lessonId, DateTimeOffset now)
    {
        var progress = _lessonProgress.FirstOrDefault(p => p.LessonId == lessonId)
            ?? throw new ArgumentException("Lesson is not part of this enrollment.", nameof(lessonId));

        var changed = progress.Complete(now);
        if (!changed)
            return; // idempotent

        if (Status == EnrollmentStatus.NotStarted)
        {
            Status = EnrollmentStatus.InProgress;
            StartedAt = now;
        }

        var completed = _lessonProgress.Count(p => p.Status == LessonProgressStatus.Completed);
        ProgressPercent = completed * 100 / _lessonProgress.Count;

        if (completed == _lessonProgress.Count)
        {
            Status = EnrollmentStatus.Completed;
            CompletedAt = now;
        }

        UpdatedAt = now;
    }
}
