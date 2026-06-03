using Lms.Application.Abstractions;
using Lms.Infrastructure.Identifiers;
using Lms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer. Called once from the API host (<c>Program.cs</c>).
/// This is the only seam where Infrastructure is wired, keeping Domain/Application free of it.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<IIdGenerator, Uuid7IdGenerator>();
        return services;
    }
}
