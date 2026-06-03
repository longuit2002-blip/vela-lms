using FluentValidation;

namespace Lms.Application.Organizations.Commands.CreateOrganization;

/// <summary>
/// Validates raw input shape. The Domain factory still enforces invariants and normalizes the
/// slug (e.g. lowercasing) — this validator stays lenient on case but rejects whitespace/symbols.
/// </summary>
public sealed class CreateOrganizationValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("Slug may contain only letters, digits, and hyphens.");
    }
}
