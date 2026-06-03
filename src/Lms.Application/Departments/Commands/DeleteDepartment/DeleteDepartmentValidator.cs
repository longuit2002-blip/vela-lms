using FluentValidation;

namespace Lms.Application.Departments.Commands.DeleteDepartment;

public sealed class DeleteDepartmentValidator : AbstractValidator<DeleteDepartmentCommand>
{
    public DeleteDepartmentValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}
