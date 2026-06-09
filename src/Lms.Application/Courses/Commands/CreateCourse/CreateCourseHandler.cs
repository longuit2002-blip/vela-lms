using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Courses.Dtos;
using Lms.Domain.Courses;
using Mediator;

namespace Lms.Application.Courses.Commands.CreateCourse;

public sealed class CreateCourseHandler(
    ICourseRepository repository,
    IIdGenerator idGenerator,
    ITenantContext tenant)
    : IRequestHandler<CreateCourseCommand, Result<CourseDto>>
{
    public async ValueTask<Result<CourseDto>> Handle(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var course = Course.Create(idGenerator.NewId(), tenant.OrganizationId, command.Title, command.Slug);

        await repository.AddAsync(course, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Created(course.ToDto());
    }
}
