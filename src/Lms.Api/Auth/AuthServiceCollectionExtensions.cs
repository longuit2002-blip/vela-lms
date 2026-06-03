using Lms.Api.Identity;
using Lms.Application.Abstractions;
using Lms.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Lms.Api;

/// <summary>
/// Composition-root wiring for auth: binds and validates the auth option sections and registers the
/// RSA key provider. Option binding lives in the API host (it depends on the configuration-binding
/// extensions the web SDK provides); the option types, validator, and key provider live in
/// Infrastructure. Later units add JwtBearer authentication and the tenant middleware here / in Program.
/// </summary>
public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<LockoutOptions>()
            .Bind(configuration.GetSection(LockoutOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SeedOptions>, SeedOptionsValidator>();

        services.AddSingleton<RsaKeyProvider>();

        // Tenant/user context resolved from JWT claims, shared by endpoints, the DbContext, and the
        // tenant connection interceptor.
        services.AddHttpContextAccessor();
        services.AddScoped<HttpTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<HttpTenantContext>());
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpTenantContext>());

        return services;
    }
}
