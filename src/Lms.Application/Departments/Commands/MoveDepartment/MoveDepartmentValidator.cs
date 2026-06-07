using FluentValidation;

namespace Lms.Application.Departments.Commands.MoveDepartment;

public sealed class MoveDepartmentValidator : AbstractValidator<MoveDepartmentCommand>
{
    public MoveDepartmentValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}
