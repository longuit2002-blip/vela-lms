using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Learning.Dtos;
using Mediator;

namespace Lms.Application.Learning.Commands.CompleteLesson;

public sealed class CompleteLessonHandler(
    IEnrollmentRepository enrollments,
    ICurrentUser currentUser,
    ITenantContext tenant)
    : IRequestHandler<CompleteLessonCommand, Result<CompleteLessonResultDto>>
{
    public async ValueTask<Result<CompleteLessonResultDto>> Handle(CompleteLessonCommand command, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var enrollment = await enrollments.FindByIdAsync(command.EnrollmentId, cancellationToken);
        // Ownership: a foreign (or cross-tenant) enrollment reads as 404, never 403.
        if (enrollment is null || enrollment.UserId != currentUser.UserId)
            return Result.NotFound("Enrollment not found.");

        // Membership check against the enrollment's own loaded progress rows — no separate course/lesson
        // lookup, so a lesson id from a different enrollment can't be probed for existence.
        if (enrollment.LessonProgress.All(p => p.LessonId != command.LessonId))
            return Result.NotFound("Lesson is not part of this enrollment.");

        enrollment.CompleteLesson(command.LessonId, DateTimeOffset.UtcNow);
        await enrollments.SaveChangesAsync(cancellationToken);

        return Result.Success(new CompleteLessonResultDto(
            enrollment.Id, command.LessonId, enrollment.ProgressPercent, enrollment.Status.ToString()));
    }
}
