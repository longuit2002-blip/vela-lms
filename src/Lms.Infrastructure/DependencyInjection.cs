using Microsoft.Extensions.DependencyInjection;

namespace Lms.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer. Called once from the API host (<c>Program.cs</c>).
/// This is the only seam where Infrastructure is wired, keeping Domain/Application free of it.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // U5 registers the Npgsql DbContext, OrganizationRepository, IIdGenerator, and health checks here.
        return services;
    }
}
