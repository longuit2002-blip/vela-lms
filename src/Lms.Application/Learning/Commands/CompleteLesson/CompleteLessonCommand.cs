using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Learning.Dtos;
using Mediator;

namespace Lms.Application.Learning.Commands.CompleteLesson;

/// <summary>Marks a lesson complete within one of the caller's enrollments and recomputes progress.</summary>
public sealed record CompleteLessonCommand(Guid EnrollmentId, Guid LessonId)
    : IRequest<Result<CompleteLessonResultDto>>, IRequirePermission
{
    public string Permission => Permissions.Learning.Consume;
}
