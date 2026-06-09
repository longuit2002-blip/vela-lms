using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Publishing.Dtos;
using Mediator;

namespace Lms.Application.Publishing.Commands.CreatePublication;

/// <summary>Creates a Draft publication for a course.</summary>
public sealed record CreatePublicationCommand(Guid CourseId, string Title)
    : IRequest<Result<PublicationDto>>, IRequirePermission
{
    public string Permission => Permissions.Publications.Create;
}
