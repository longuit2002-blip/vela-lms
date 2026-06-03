using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lms.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations add</c> can build the context without a running
/// host or full configuration. EF does not connect during <c>migrations add</c>, so the connection
/// string only needs to be parseable; the env var override keeps it flexible.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=lms;Username=lms;Password=lms_local_dev";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
