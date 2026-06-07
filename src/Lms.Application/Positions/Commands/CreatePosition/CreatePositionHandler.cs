using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Positions.Dtos;
using Lms.Domain.Positions;
using Mediator;

namespace Lms.Application.Positions.Commands.CreatePosition;

public sealed class CreatePositionHandler(
    IPositionRepository repository,
    IIdGenerator idGenerator,
    ITenantContext tenant)
    : IRequestHandler<CreatePositionCommand, Result<PositionDto>>
{
    public async ValueTask<Result<PositionDto>> Handle(CreatePositionCommand command, CancellationToken cancellationToken)
    {
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        // Domain normalizes the name; check uniqueness on the normalized value (mirrors CreateOrganization).
        var position = Position.Create(idGenerator.NewId(), tenant.OrganizationId, command.Name);

        if (await repository.NameExistsAsync(tenant.OrganizationId, position.Name, cancellationToken))
            return Result.Conflict($"A position named '{position.Name}' already exists.");

        await repository.AddAsync(position, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Created(position.ToDto());
    }
}
