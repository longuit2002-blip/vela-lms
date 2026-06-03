using Microsoft.Extensions.DependencyInjection;

namespace Lms.Application;

/// <summary>
/// Composition root for the Application layer. Called once from the API host (<c>Program.cs</c>).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // U4 registers the martinothamar Mediator (source-gen), FluentValidation validators,
        // and the ValidationBehavior pipeline here.
        return services;
    }
}
