using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Courses.Dtos;
using Mediator;

namespace Lms.Application.Courses.Commands.AddModule;

public sealed class AddModuleHandler(
    ICourseRepository repository,
    IIdGenerator idGenerator,
    ITenantContext tenant)
    : IRequestHandler<AddModuleCommand, Result<ModuleDto>>
{
    public async ValueTask<Result<ModuleDto>> Handle(AddModuleCommand command, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var course = await repository.FindByIdAsync(command.CourseId, cancellationToken);
        if (course is null)
            return Result.NotFound("Course not found.");

        var module = course.AddModule(idGenerator.NewId(), command.Title, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Created(module.ToDto());
    }
}
