using FluentValidation;
using Mediator;

namespace Lms.Application.Behaviors;

/// <summary>
/// Runs FluentValidation validators before the handler. On failure it throws a
/// <see cref="ValidationException"/>, which the API maps to a 422 problem+json (U6) — so the
/// invalid branch never relies on <c>Result.Invalid</c>/<c>.ToMinimalApiResult()</c> (which would 400).
/// </summary>
public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TMessage>(message);
            var results = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = results.SelectMany(r => r.Errors).ToList();
            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next(message, cancellationToken);
    }
}
