using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Positions.Dtos;
using Mediator;

namespace Lms.Application.Positions.Commands.RenamePosition;

public sealed class RenamePositionHandler(IPositionRepository repository)
    : IRequestHandler<RenamePositionCommand, Result<PositionDto>>
{
    public async ValueTask<Result<PositionDto>> Handle(RenamePositionCommand command, CancellationToken cancellationToken)
    {
        var position = await repository.FindByIdAsync(command.PositionId, cancellationToken);
        if (position is null)
            return Result.NotFound();

        // Reject a clash with a different position; renaming to the same name is a no-op, not a conflict.
        var newName = command.Name.Trim();
        if (!string.Equals(newName, position.Name, StringComparison.Ordinal)
            && await repository.NameExistsAsync(position.OrganizationId, newName, cancellationToken))
        {
            return Result.Conflict($"A position named '{newName}' already exists.");
        }

        position.Rename(command.Name, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(position.ToDto());
    }
}
