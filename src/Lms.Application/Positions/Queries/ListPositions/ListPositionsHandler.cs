using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Positions.Dtos;
using Mediator;

namespace Lms.Application.Positions.Queries.ListPositions;

public sealed class ListPositionsHandler(IPositionRepository repository, ITenantContext tenant)
    : IRequestHandler<ListPositionsQuery, Result<IReadOnlyList<PositionDto>>>
{
    public async ValueTask<Result<IReadOnlyList<PositionDto>>> Handle(ListPositionsQuery query, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var positions = await repository.ListByOrganizationAsync(cancellationToken);
        IReadOnlyList<PositionDto> dtos = [.. positions.Select(p => p.ToDto())];
        return Result.Success(dtos);
    }
}
