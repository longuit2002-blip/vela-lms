namespace Lms.Domain.Courses;

/// <summary>
/// Kind of content a <see cref="Lesson"/> carries. Only <see cref="Video"/> is supported in the
/// first learning-loop slice; future types (Document, Scorm) are added at their own slice.
/// Maps to a plain integer column — no table-per-hierarchy implications.
/// </summary>
public enum LessonType
{
    Video = 0,
}
