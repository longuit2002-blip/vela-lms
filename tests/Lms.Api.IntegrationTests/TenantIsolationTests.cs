using Lms.Domain.Organizations;
using Lms.Domain.Users;
using Lms.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lms.Api.IntegrationTests;

/// <summary>
/// Proves the RLS scaffold (Covers F3 / AE5). Two orgs each with a user are seeded via the superuser
/// connection (which bypasses RLS). The non-owner <c>lms_app</c> role — the role actually SUBJECT to
/// the policy — then demonstrates: per-tenant visibility, fail-closed when no tenant is set, and the
/// role guard that catches a false-green (the superuser, by contrast, sees everything).
/// </summary>
[Collection(nameof(IntegrationCollection))]
public sealed class TenantIsolationTests(WebAppFactory factory)
{
    [Fact] // Covers F3 / AE5
    public async Task Rls_isolates_users_by_tenant_for_the_subject_role()
    {
        var (orgA, userA) = (Guid.NewGuid(), Guid.NewGuid());
        var (orgB, userB) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedOrgWithUserAsync(orgA, userA, "iso-a");
        await SeedOrgWithUserAsync(orgB, userB, "iso-b");

        // As the RLS-subject app role, scoped to org A: org A's user is visible, org B's is not.
        Assert.Equal(1, await CountUserAsync(factory.AppRoleConnectionString, userA, tenant: orgA));
        Assert.Equal(0, await CountUserAsync(factory.AppRoleConnectionString, userB, tenant: orgA));

        // Scoped to org B: the reverse.
        Assert.Equal(1, await CountUserAsync(factory.AppRoleConnectionString, userB, tenant: orgB));
        Assert.Equal(0, await CountUserAsync(factory.AppRoleConnectionString, userA, tenant: orgB));

        // Fail closed: with no tenant set, the subject role sees nothing.
        Assert.Equal(0, await CountUserAsync(factory.AppRoleConnectionString, userA, tenant: null));

        // Role guard (catches a false-green): the superuser bypasses RLS and sees the row even with no
        // tenant set — which is exactly why the assertions above MUST run as lms_app, not the superuser.
        Assert.Equal(1, await CountUserAsync(factory.ConnectionString, userA, tenant: null));
    }

    private async Task SeedOrgWithUserAsync(Guid orgId, Guid userId, string slug)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Construct entities with explicit ids (factories normally take a generated id).
        var org = Organization.Create(orgId, $"Org {slug}", slug);
        var user = User.Create(userId, orgId, $"user-{slug}@vela.local", "$argon2id$seed", ["OrgOwner"], false);

        db.Organizations.Add(org);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private static async Task<int> CountUserAsync(string connectionString, Guid userId, Guid? tenant)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        if (tenant is { } org)
        {
            await using var set = conn.CreateCommand();
            set.CommandText = "SELECT set_config('app.current_org', @org, false)";
            set.Parameters.AddWithValue("org", org.ToString());
            await set.ExecuteNonQueryAsync();
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM users WHERE id = @id";
        cmd.Parameters.AddWithValue("id", userId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
}
