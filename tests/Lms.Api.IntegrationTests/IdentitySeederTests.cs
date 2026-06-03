using System.Net;
using System.Net.Http.Json;
using Lms.Application.Abstractions;
using Lms.Application.Auth;
using Lms.Infrastructure.Persistence;
using Lms.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lms.Api.IntegrationTests;

[Collection(nameof(IntegrationCollection))]
public sealed class IdentitySeederTests(WebAppFactory factory)
{
    private const string SeedPassword = "Seeded-Owner-Pass-123!";

    [Fact] // Covers R13
    public async Task Seeder_creates_one_org_and_owner_idempotently_and_owner_can_log_in()
    {
        var slug = "seed-" + Guid.NewGuid().ToString("N")[..8];
        var email = $"owner-{slug}@vela.local";
        var options = Options.Create(new SeedOptions
        {
            Enabled = true,
            OrganizationName = "Seed Org",
            OrganizationSlug = slug,
            OwnerEmail = email,
            OwnerPassword = SeedPassword,
        });

        await RunSeederAsync(options);
        await RunSeederAsync(options); // second run must be a no-op (idempotent)

        // Exactly one owner, flagged must-change.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var owners = await db.Users.IgnoreQueryFilters().Where(u => u.Email == email).ToListAsync();
            Assert.Single(owners);
            Assert.True(owners[0].MustChangePassword);
            Assert.Contains(IdentitySeeder.OrgOwnerRole, owners[0].RoleCodes);

            var orgs = await db.Organizations.Where(o => o.Slug == slug).ToListAsync();
            Assert.Single(orgs);
        }

        // The seeded owner can authenticate, and login reports the forced-change requirement.
        var login = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email, password = SeedPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<AuthBody>();
        Assert.True(body!.MustChangePassword);
    }

    [Fact]
    public async Task Seeder_is_a_no_op_when_disabled()
    {
        var slug = "disabled-" + Guid.NewGuid().ToString("N")[..8];
        var options = Options.Create(new SeedOptions
        {
            Enabled = false,
            OrganizationSlug = slug,
            OwnerEmail = $"none-{slug}@vela.local",
            OwnerPassword = SeedPassword,
        });

        await RunSeederAsync(options);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.Organizations.Where(o => o.Slug == slug).ToListAsync());
    }

    private async Task RunSeederAsync(IOptions<SeedOptions> options)
    {
        using var scope = factory.Services.CreateScope();
        var seeder = new IdentitySeeder(
            scope.ServiceProvider.GetRequiredService<IOrganizationRepository>(),
            scope.ServiceProvider.GetRequiredService<IUserRepository>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            scope.ServiceProvider.GetRequiredService<IIdGenerator>(),
            options,
            NullLogger<IdentitySeeder>.Instance);
        await seeder.SeedAsync();
    }

    private sealed record AuthBody(string AccessToken, DateTimeOffset AccessTokenExpiresAt, bool MustChangePassword);
}
