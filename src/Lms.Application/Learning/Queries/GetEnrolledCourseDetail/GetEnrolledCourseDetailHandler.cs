using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Learning.Dtos;
using Lms.Domain.Learning;
using Mediator;

namespace Lms.Application.Learning.Queries.GetEnrolledCourseDetail;

public sealed class GetEnrolledCourseDetailHandler(
    IEnrollmentRepository enrollments,
    IPublicationRepository publications,
    ICourseRepository courses,
    ICurrentUser currentUser,
    ITenantContext tenant)
    : IRequestHandler<GetEnrolledCourseDetailQuery, Result<EnrolledCourseDetailDto>>
{
    public async ValueTask<Result<EnrolledCourseDetailDto>> Handle(GetEnrolledCourseDetailQuery query, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var enrollment = await enrollments.FindByIdAsync(query.EnrollmentId, cancellationToken);
        // Ownership: a foreign (or cross-tenant) enrollment reads as 404, never 403.
        if (enrollment is null || enrollment.UserId != currentUser.UserId)
            return Result.NotFound("Enrollment not found.");

        var publication = await publications.FindByIdAsync(enrollment.PublicationId, cancellationToken);
        if (publication is null)
            return Result.NotFound("Publication not found.");

        var course = await courses.FindByIdAsync(publication.ContentId, cancellationToken);
        if (course is null)
            return Result.NotFound("Course not found.");

        var completedLessonIds = enrollment.LessonProgress
            .Where(p => p.Status == LessonProgressStatus.Completed)
            .Select(p => p.LessonId)
            .ToHashSet();

        var modules = course.Modules
            .OrderBy(m => m.Order)
            .Select(m => new EnrolledModuleDto(
                m.Id, m.Title, m.Order,
                m.Lessons
                    .OrderBy(l => l.Order)
                    .Select(l => new EnrolledLessonDto(l.Id, l.Title, l.Order, l.VideoUrl, l.DurationSeconds, completedLessonIds.Contains(l.Id)))
                    .ToList()))
            .ToList();

        return Result.Success(new EnrolledCourseDetailDto(
            enrollment.Id, course.Title, enrollment.Status.ToString(), enrollment.ProgressPercent, modules));
    }
}
