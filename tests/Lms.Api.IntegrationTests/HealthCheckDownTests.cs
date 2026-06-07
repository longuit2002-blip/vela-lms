using System.Net;

namespace Lms.Api.IntegrationTests;

/// <summary>
/// AE5: when Postgres is down, readiness reports unhealthy (503) while liveness stays healthy (200).
/// Uses its own factory/container so stopping the database does not affect the shared endpoint tests.
/// </summary>
public sealed class HealthCheckDownTests
{
    [Fact] // Covers AE5
    public async Task Ready_is_unhealthy_when_postgres_down_but_live_stays_healthy()
    {
        await using var factory = new WebAppFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        // Sanity: ready is healthy while the database is up.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);

        await factory.StopDatabaseAsync();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
    }
}
