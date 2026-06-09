using Ardalis.Result;
using FluentValidation;
using Lms.Application.Abstractions;
using Lms.Application.Publishing.Dtos;
using Lms.Domain.Publishing;
using Mediator;

namespace Lms.Application.Publishing.Commands.PublishPublication;

public sealed class PublishPublicationHandler(
    IPublicationRepository publications,
    ICourseRepository courses,
    ICurrentUser currentUser,
    ITenantContext tenant)
    : IRequestHandler<PublishPublicationCommand, Result<PublicationDto>>
{
    public async ValueTask<Result<PublicationDto>> Handle(PublishPublicationCommand command, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var publication = await publications.FindByIdAsync(command.PublicationId, cancellationToken);
        if (publication is null)
            return Result.NotFound("Publication not found.");

        if (publication.Status != PublicationStatus.Draft)
            return Result.Conflict("Publication is not in a draft state.");

        var course = await courses.FindByIdAsync(publication.ContentId, cancellationToken);
        if (course is null)
            return Result.NotFound("Course not found.");

        // Cross-aggregate publish invariant. Thrown as a validation error so it surfaces as 422
        // (the only path GlobalExceptionHandler maps to 422); a handler Result would map to 409/400.
        if (!course.HasAnyLesson)
            throw new ValidationException("A course must have at least one lesson to be published.");

        publication.Publish(currentUser.UserId, DateTimeOffset.UtcNow);
        await publications.SaveChangesAsync(cancellationToken);

        return Result.Success(publication.ToDto());
    }
}
