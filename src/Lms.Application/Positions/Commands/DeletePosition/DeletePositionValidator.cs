using FluentValidation;

namespace Lms.Application.Positions.Commands.DeletePosition;

public sealed class DeletePositionValidator : AbstractValidator<DeletePositionCommand>
{
    public DeletePositionValidator()
    {
        RuleFor(x => x.PositionId).NotEmpty();
    }
}
