using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Positions.Dtos;
using Mediator;

namespace Lms.Application.Positions.Commands.CreatePosition;

public sealed record CreatePositionCommand(string Name) : IRequest<Result<PositionDto>>, IRequirePermission
{
    public string Permission => Permissions.Positions.Manage;
}
