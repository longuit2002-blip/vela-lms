using FluentValidation;

namespace Lms.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(256)
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from the current password.");
    }
}
