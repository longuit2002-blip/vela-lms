using FluentValidation;

namespace Lms.Application.Positions.Commands.CreatePosition;

public sealed class CreatePositionValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
