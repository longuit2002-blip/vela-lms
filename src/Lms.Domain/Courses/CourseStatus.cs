namespace Lms.Domain.Courses;

/// <summary>
/// Authoring lifecycle of a <see cref="Course"/>. The publish lifecycle that gates learner
/// visibility lives on the Publication aggregate; a course stays <see cref="Draft"/> through the
/// first learning-loop slice (no transitions are driven here yet).
/// </summary>
public enum CourseStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}
