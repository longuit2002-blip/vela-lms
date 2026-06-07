using FluentValidation;

namespace Lms.Application.Positions.Commands.RenamePosition;

public sealed class RenamePositionValidator : AbstractValidator<RenamePositionCommand>
{
    public RenamePositionValidator()
    {
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
