using FluentValidation;

namespace Lms.Application.Courses.Commands.AddModule;

public sealed class AddModuleValidator : AbstractValidator<AddModuleCommand>
{
    public AddModuleValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
    }
}
