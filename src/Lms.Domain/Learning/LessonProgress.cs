using Lms.Domain.SeedWork;

namespace Lms.Domain.Learning;

/// <summary>
/// Tracks a learner's completion of one lesson within an <see cref="Enrollment"/>. An entity inside
/// the Enrollment aggregate — created and mutated only through the <see cref="Enrollment"/> root.
/// </summary>
public sealed class LessonProgress : Entity
{
    public Guid EnrollmentId { get; private set; }
    public Guid LessonId { get; private set; }
    public LessonProgressStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Required by EF Core for materialization.
    private LessonProgress() { }

    internal LessonProgress(Guid id, Guid enrollmentId, Guid lessonId)
    {
        Id = id;
        EnrollmentId = enrollmentId;
        LessonId = lessonId;
        Status = LessonProgressStatus.NotStarted;
    }

    internal bool Complete(DateTimeOffset now)
    {
        if (Status == LessonProgressStatus.Completed)
            return false; // idempotent — already complete

        Status = LessonProgressStatus.Completed;
        CompletedAt = now;
        return true;
    }
}
