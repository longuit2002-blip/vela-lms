using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Learning.Dtos;
using Mediator;

namespace Lms.Application.Learning.Queries.GetMyEnrollments;

public sealed class GetMyEnrollmentsHandler(
    IEnrollmentRepository enrollments,
    IPublicationRepository publications,
    ICourseRepository courses,
    ICurrentUser currentUser,
    ITenantContext tenant)
    : IRequestHandler<GetMyEnrollmentsQuery, Result<IReadOnlyList<EnrollmentSummaryDto>>>
{
    public async ValueTask<Result<IReadOnlyList<EnrollmentSummaryDto>>> Handle(GetMyEnrollmentsQuery query, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var mine = await enrollments.ListByUserAsync(currentUser.UserId, cancellationToken);
        if (mine.Count == 0)
            return Result.Success<IReadOnlyList<EnrollmentSummaryDto>>([]);

        var pubs = (await publications.ListByIdsAsync([.. mine.Select(e => e.PublicationId).Distinct()], cancellationToken))
            .ToDictionary(p => p.Id);
        var courseList = (await courses.ListByIdsAsync([.. pubs.Values.Select(p => p.ContentId).Distinct()], cancellationToken))
            .ToDictionary(c => c.Id);

        var summaries = new List<EnrollmentSummaryDto>();
        foreach (var e in mine)
        {
            if (!pubs.TryGetValue(e.PublicationId, out var pub) || !courseList.TryGetValue(pub.ContentId, out var course))
                continue; // publication/course no longer resolvable — skip from the dashboard

            summaries.Add(new EnrollmentSummaryDto(
                e.Id, e.PublicationId, course.Title, course.Slug, e.ProgressPercent, e.Status.ToString()));
        }

        return Result.Success<IReadOnlyList<EnrollmentSummaryDto>>(summaries);
    }
}
