using FluentValidation;

namespace Lms.Application.Departments.Commands.RenameDepartment;

public sealed class RenameDepartmentValidator : AbstractValidator<RenameDepartmentCommand>
{
    public RenameDepartmentValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
