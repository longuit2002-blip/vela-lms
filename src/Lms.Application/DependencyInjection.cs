using FluentValidation;
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
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

        // FluentValidation validators for command/query input.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        // Validation runs as a pipeline behavior before every handler.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
