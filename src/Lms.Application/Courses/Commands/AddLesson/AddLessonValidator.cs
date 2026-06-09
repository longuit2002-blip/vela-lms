using FluentValidation;

namespace Lms.Application.Courses.Commands.AddLesson;

public sealed class AddLessonValidator : AbstractValidator<AddLessonCommand>
{
    public AddLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DurationSeconds).GreaterThan(0);
        RuleFor(x => x.VideoUrl)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(BeHttpsUrl).WithMessage("Video URL must be an absolute https URL.");
    }

    private static bool BeHttpsUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
