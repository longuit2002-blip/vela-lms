using Lms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        });
    }

    /// <summary>Stops the database container to exercise the readiness failure path (AE5).</summary>
    public Task StopDatabaseAsync() => _postgres.StopAsync();

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
