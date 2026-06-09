using FluentValidation;

namespace Lms.Application.Publishing.Commands.CreatePublication;

public sealed class CreatePublicationValidator : AbstractValidator<CreatePublicationCommand>
{
    public CreatePublicationValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
    }
}
