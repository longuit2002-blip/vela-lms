using Ardalis.Result;
using Lms.Application.Authorization;
using Lms.Application.Positions.Dtos;
using Mediator;

namespace Lms.Application.Positions.Queries.ListPositions;

public sealed record ListPositionsQuery : IRequest<Result<IReadOnlyList<PositionDto>>>, IRequirePermission
{
    public string Permission => Permissions.Positions.Read;
}
