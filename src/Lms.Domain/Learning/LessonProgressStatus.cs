namespace Lms.Domain.Learning;

/// <summary>
/// Per-lesson completion state within an <see cref="Enrollment"/>. Watch-ratio tracking (an
/// intermediate "in progress" with partial watch) is deferred to the media slice.
/// </summary>
public enum LessonProgressStatus
{
    NotStarted = 0,
    Completed = 1,
}
