using Lms.Domain.Courses;

namespace Lms.Application.Courses.Dtos;

/// <summary>Read model for a course with its ordered modules and lessons.</summary>
public sealed record CourseDto(Guid Id, string Title, string Slug, string Status, IReadOnlyList<ModuleDto> Modules);

public sealed record ModuleDto(Guid Id, string Title, int Order, IReadOnlyList<LessonDto> Lessons);

public sealed record LessonDto(Guid Id, string Title, int Order, string Type, string VideoUrl, int DurationSeconds);

/// <summary>Manual mapping (no AutoMapper) from the Course aggregate to its DTOs, ordered by Order.</summary>
public static class CourseMappings
{
    public static CourseDto ToDto(this Course course) =>
        new(
            course.Id,
            course.Title,
            course.Slug,
            course.Status.ToString(),
            course.Modules
                .OrderBy(m => m.Order)
                .Select(m => m.ToDto())
                .ToList());

    public static ModuleDto ToDto(this Module module) =>
        new(
            module.Id,
            module.Title,
            module.Order,
            module.Lessons
                .OrderBy(l => l.Order)
                .Select(l => l.ToDto())
                .ToList());

    public static LessonDto ToDto(this Lesson lesson) =>
        new(lesson.Id, lesson.Title, lesson.Order, lesson.Type.ToString(), lesson.VideoUrl, lesson.DurationSeconds);
}
