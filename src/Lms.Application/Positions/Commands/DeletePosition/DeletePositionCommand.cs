using Ardalis.Result;
using Lms.Application.Authorization;
using Mediator;

namespace Lms.Application.Positions.Commands.DeletePosition;

public sealed record DeletePositionCommand(Guid PositionId) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.Positions.Manage;
}
