using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Courses.Dtos;
using Mediator;

namespace Lms.Application.Courses.Commands.CreateCourse;

/// <summary>Creates a Draft course in the caller's organization.</summary>
public sealed record CreateCourseCommand(string Title, string Slug)
    : IRequest<Result<CourseDto>>, IRequirePermission
{
    public string Permission => Permissions.Courses.Create;
}
