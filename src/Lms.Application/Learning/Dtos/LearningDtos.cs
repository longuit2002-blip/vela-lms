namespace Lms.Application.Learning.Dtos;

/// <summary>A learner's assigned course as shown on the dashboard queue.</summary>
public sealed record EnrollmentSummaryDto(
    Guid EnrollmentId,
    Guid PublicationId,
    string CourseTitle,
    string CourseSlug,
    int ProgressPercent,
    string Status);

/// <summary>The course a learner opens from an enrollment, with per-lesson completion state.</summary>
public sealed record EnrolledCourseDetailDto(
    Guid EnrollmentId,
    string CourseTitle,
    string Status,
    int ProgressPercent,
    IReadOnlyList<EnrolledModuleDto> Modules);

public sealed record EnrolledModuleDto(Guid Id, string Title, int Order, IReadOnlyList<EnrolledLessonDto> Lessons);

public sealed record EnrolledLessonDto(Guid Id, string Title, int Order, string VideoUrl, int DurationSeconds, bool Completed);

/// <summary>Result of marking a lesson complete — the recomputed enrollment progress.</summary>
public sealed record CompleteLessonResultDto(Guid EnrollmentId, Guid LessonId, int ProgressPercent, string Status);
