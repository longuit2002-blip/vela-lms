using FluentValidation;

namespace Lms.Application.Publishing.Commands.AssignPublication;

public sealed class AssignPublicationValidator : AbstractValidator<AssignPublicationCommand>
{
    /// <summary>Upper bound on a single assign batch — bounds the synchronous transaction and the existence-probe surface.</summary>
    public const int MaxBatchSize = 500;

    public AssignPublicationValidator()
    {
        RuleFor(x => x.PublicationId).NotEmpty();
        RuleFor(x => x.UserIds).NotEmpty();
        RuleFor(x => x.UserIds).Must(ids => ids is null || ids.Count <= MaxBatchSize)
            .WithMessage($"Cannot assign to more than {MaxBatchSize} learners in one request.");
    }
}
