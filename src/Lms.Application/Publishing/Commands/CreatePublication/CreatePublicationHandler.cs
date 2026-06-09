using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Publishing.Dtos;
using Lms.Domain.Publishing;
using Mediator;

namespace Lms.Application.Publishing.Commands.CreatePublication;

public sealed class CreatePublicationHandler(
    IPublicationRepository publications,
    ICourseRepository courses,
    IIdGenerator idGenerator,
    ITenantContext tenant)
    : IRequestHandler<CreatePublicationCommand, Result<PublicationDto>>
{
    public async ValueTask<Result<PublicationDto>> Handle(CreatePublicationCommand command, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var course = await courses.FindByIdAsync(command.CourseId, cancellationToken);
        if (course is null)
            return Result.NotFound("Course not found.");

        var publication = Publication.CreateForCourse(idGenerator.NewId(), tenant.OrganizationId, course.Id, command.Title);

        await publications.AddAsync(publication, cancellationToken);
        await publications.SaveChangesAsync(cancellationToken);

        return Result.Created(publication.ToDto());
    }
}
