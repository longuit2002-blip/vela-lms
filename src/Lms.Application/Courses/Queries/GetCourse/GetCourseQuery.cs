using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Courses.Dtos;
using Mediator;

namespace Lms.Application.Courses.Queries.GetCourse;

/// <summary>Reads a course (with modules and lessons) in the caller's organization.</summary>
public sealed record GetCourseQuery(Guid CourseId)
    : IRequest<Result<CourseDto>>, IRequirePermission
{
    public string Permission => Permissions.Courses.Read;
}
