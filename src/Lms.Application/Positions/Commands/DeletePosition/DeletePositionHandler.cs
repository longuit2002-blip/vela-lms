using Ardalis.Result;
using Lms.Application.Abstractions;
using Mediator;

namespace Lms.Application.Positions.Commands.DeletePosition;

public sealed class DeletePositionHandler(IPositionRepository repository)
    : IRequestHandler<DeletePositionCommand, Result>
{
    public async ValueTask<Result> Handle(DeletePositionCommand command, CancellationToken cancellationToken)
    {
        var position = await repository.FindByIdAsync(command.PositionId, cancellationToken);
        if (position is null)
            return Result.NotFound();

        if (await repository.HasAssignedUsersAsync(position.Id, cancellationToken))
            return Result.Conflict("Cannot delete a position that is held by users.");

        await repository.RemoveAsync(position, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
