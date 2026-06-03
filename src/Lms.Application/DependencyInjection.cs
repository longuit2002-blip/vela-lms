using FluentValidation;
using Lms.Application.Authorization;
using Lms.Application.Behaviors;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Application;

/// <summary>
/// Composition root for the Application layer. Called once from the API host (<c>Program.cs</c>).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // martinothamar Mediator (source-generated). Handlers resolve their ctor deps from DI.
        // PipelineBehaviors is an ORDERED, compile-time array (the source generator reads it) — this is
        // the documented way to order behaviors. Authorization runs before Validation, so an unauthorized
        // caller gets 403 and never reaches validation (no 422 input-shape leak). Do NOT switch to manual
        // AddScoped open-generic registration: martinothamar does not document that as ordered.
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors =
            [
                typeof(AuthorizationBehavior<,>),
                typeof(ValidationBehavior<,>),
            ];
        });

        // FluentValidation validators for command/query input (resolved by ValidationBehavior).
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
