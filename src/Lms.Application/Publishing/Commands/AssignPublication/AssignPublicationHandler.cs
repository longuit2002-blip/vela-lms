using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Publishing.Dtos;
using Lms.Domain.Learning;
using Lms.Domain.Publishing;
using Mediator;

namespace Lms.Application.Publishing.Commands.AssignPublication;

public sealed class AssignPublicationHandler(
    IPublicationRepository publications,
    ICourseRepository courses,
    IUserRepository users,
    IEnrollmentRepository enrollments,
    IIdGenerator idGenerator,
    ITenantContext tenant)
    : IRequestHandler<AssignPublicationCommand, Result<AssignmentResultDto>>
{
    public async ValueTask<Result<AssignmentResultDto>> Handle(AssignPublicationCommand command, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var publication = await publications.FindByIdAsync(command.PublicationId, cancellationToken);
        if (publication is null)
            return Result.NotFound("Publication not found.");

        if (publication.Status != PublicationStatus.Published)
            return Result.Conflict("Only a published publication can be assigned.");

        var course = await courses.FindByIdAsync(publication.ContentId, cancellationToken);
        if (course is null)
            return Result.NotFound("Course not found.");

        var lessonIds = course.Modules.SelectMany(m => m.Lessons).Select(l => l.Id).ToList();
        if (lessonIds.Count == 0)
            return Result.Conflict("Course has no lessons to enroll against.");

        var targetUserIds = command.UserIds.Distinct().ToList();

        // All-or-nothing: validate every target is in-tenant first (FindByIdAsync is tenant-filtered, so a
        // cross-tenant or unknown id reads as not-found). No enrollments are created if any id fails.
        foreach (var userId in targetUserIds)
        {
            if (await users.FindByIdAsync(userId, cancellationToken) is null)
                return Result.NotFound("One or more target users were not found.");
        }

        var toEnroll = new List<Enrollment>();
        var skipped = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var userId in targetUserIds)
        {
            if (await enrollments.ExistsAsync(userId, publication.Id, cancellationToken))
            {
                skipped++;
                continue;
            }

            toEnroll.Add(Enrollment.CreateAssigned(idGenerator.NewId(), tenant.OrganizationId, userId, publication.Id, lessonIds, now));
        }

        if (toEnroll.Count > 0)
        {
            await enrollments.AddRangeAsync(toEnroll, cancellationToken);
            await enrollments.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new AssignmentResultDto(publication.Id, toEnroll.Count, skipped));
    }
}
