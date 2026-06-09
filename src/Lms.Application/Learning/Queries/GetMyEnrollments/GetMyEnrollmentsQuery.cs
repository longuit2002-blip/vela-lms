using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Learning.Dtos;
using Mediator;

namespace Lms.Application.Learning.Queries.GetMyEnrollments;

/// <summary>The caller's assigned courses (enrollments) with progress, for the learner dashboard.</summary>
public sealed record GetMyEnrollmentsQuery
    : IRequest<Result<IReadOnlyList<EnrollmentSummaryDto>>>, IRequirePermission
{
    public string Permission => Permissions.Learning.Self;
}
