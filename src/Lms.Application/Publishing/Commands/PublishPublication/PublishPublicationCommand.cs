using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Publishing.Dtos;
using Mediator;

namespace Lms.Application.Publishing.Commands.PublishPublication;

/// <summary>Publishes a Draft publication. Requires the underlying course to have at least one lesson.</summary>
public sealed record PublishPublicationCommand(Guid PublicationId)
    : IRequest<Result<PublicationDto>>, IRequirePermission
{
    public string Permission => Permissions.Publications.Publish;
}
