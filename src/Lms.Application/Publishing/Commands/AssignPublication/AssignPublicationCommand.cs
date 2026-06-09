using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Publishing.Dtos;
using Mediator;

namespace Lms.Application.Publishing.Commands.AssignPublication;

/// <summary>
/// Assigns a published publication to one or more learners, creating an Enrollment each. Temporary
/// scaffold for publish-to-audience fan-out — replaced by audience targeting in a later slice.
/// </summary>
public sealed record AssignPublicationCommand(Guid PublicationId, IReadOnlyList<Guid> UserIds)
    : IRequest<Result<AssignmentResultDto>>, IRequirePermission
{
    public string Permission => Permissions.Assignments.Create;
}
