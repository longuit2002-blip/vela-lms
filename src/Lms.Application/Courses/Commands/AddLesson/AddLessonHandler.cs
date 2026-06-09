using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Courses.Dtos;
using Mediator;

namespace Lms.Application.Courses.Commands.AddLesson;

public sealed class AddLessonHandler(
    ICourseRepository repository,
    IIdGenerator idGenerator,
    ITenantContext tenant)
    : IRequestHandler<AddLessonCommand, Result<LessonDto>>
{
    public async ValueTask<Result<LessonDto>> Handle(AddLessonCommand command, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var course = await repository.FindByIdAsync(command.CourseId, cancellationToken);
        if (course is null)
            return Result.NotFound("Course not found.");

        if (course.Modules.All(m => m.Id != command.ModuleId))
            return Result.NotFound("Module not found.");

        var lesson = course.AddLesson(
            command.ModuleId, idGenerator.NewId(), command.Title, command.VideoUrl, command.DurationSeconds, DateTimeOffset.UtcNow);
        repository.AddLesson(lesson);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Created(lesson.ToDto());
    }
}
