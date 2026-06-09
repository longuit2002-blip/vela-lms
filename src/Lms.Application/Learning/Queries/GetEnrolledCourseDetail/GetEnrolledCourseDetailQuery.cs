using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Learning.Dtos;
using Mediator;

namespace Lms.Application.Learning.Queries.GetEnrolledCourseDetail;

/// <summary>The course behind one of the caller's enrollments, with per-lesson completion state.</summary>
public sealed record GetEnrolledCourseDetailQuery(Guid EnrollmentId)
    : IRequest<Result<EnrolledCourseDetailDto>>, IRequirePermission
{
    public string Permission => Permissions.Learning.Consume;
}
