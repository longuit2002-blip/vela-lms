using Lms.Infrastructure.Persistence;
using Lms.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Lms.Api.IntegrationTests;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL container (Testcontainers) and applies
/// migrations. The app's DbContext registration is replaced via <c>ConfigureTestServices</c> so the
/// container connection string is used regardless of appsettings precedence in minimal hosting.
/// </summary>
public sealed class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    /// <summary>Superuser connection string for the container (the role the app currently connects as).</summary>
    public string ConnectionString => _postgres.GetConnectionString();

    /// <summary>
    /// Connection string for the non-owner <c>lms_app</c> role created by the RLS migration — the
    /// role that is actually SUBJECT to row-level security. Isolation tests use this to prove the policy.
    /// </summary>
    public string AppRoleConnectionString => new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
    {
        Username = "lms_app",
        Password = "lms_app_local_dev",
    }.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // Accessing Services builds the host (with the container DbContext), then migrate.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Capture the connection string once while the container is up — calling GetConnectionString()
        // lazily would throw "port not mapped" once the container is stopped (AE5).
        var connectionString = _postgres.GetConnectionString();
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<AppDbContext>((sp, options) => options
                .UseNpgsql(connectionString)
                .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));
        });
    }

    /// <summary>Stops the database container to exercise the readiness failure path (AE5).</summary>
    public Task StopDatabaseAsync() => _postgres.StopAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
