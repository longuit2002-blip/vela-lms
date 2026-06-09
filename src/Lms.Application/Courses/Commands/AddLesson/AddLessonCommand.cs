using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Courses.Dtos;
using Mediator;

namespace Lms.Application.Courses.Commands.AddLesson;

/// <summary>Appends a video lesson to a module of a Draft course.</summary>
public sealed record AddLessonCommand(Guid CourseId, Guid ModuleId, string Title, string VideoUrl, int DurationSeconds)
    : IRequest<Result<LessonDto>>, IRequirePermission
{
    public string Permission => Permissions.Courses.Update;
}
