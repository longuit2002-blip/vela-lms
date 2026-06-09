using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Courses.Dtos;
using Mediator;

namespace Lms.Application.Courses.Queries.GetCourse;

public sealed class GetCourseHandler(ICourseRepository repository, ITenantContext tenant)
    : IRequestHandler<GetCourseQuery, Result<CourseDto>>
{
    public async ValueTask<Result<CourseDto>> Handle(GetCourseQuery query, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var course = await repository.FindByIdAsync(query.CourseId, cancellationToken);
        return course is null
            ? Result.NotFound("Course not found.")
            : Result.Success(course.ToDto());
    }
}
