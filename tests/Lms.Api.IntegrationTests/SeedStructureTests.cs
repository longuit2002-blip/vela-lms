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
public sealed class SeedStructureTests(WebAppFactory factory)
{
    private const string Password = "Seeded-Owner-Pass-123!";

    [Fact] // Covers R14
    public async Task Seeds_tree_positions_and_branch_scoped_accounts_idempotently()
    {
        var slug = "struct-" + Guid.NewGuid().ToString("N")[..8];
        var options = Options.Create(new SeedOptions
        {
            Enabled = true,
            OrganizationName = "Structure Org",
            OrganizationSlug = slug,
            OwnerEmail = $"owner-{slug}@vela.local",
            OwnerPassword = Password,
        });

        await RunSeederAsync(options);
        await RunSeederAsync(options); // second run must be a no-op (idempotent)

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var org = await db.Organizations.FirstAsync(o => o.Slug == slug);

        // Tree: Sales (root) + Engineering (root) + Sales/East + Sales/West.
        var depts = await db.Departments.IgnoreQueryFilters().Where(d => d.OrganizationId == org.Id).ToListAsync();
        Assert.Equal(4, depts.Count);
        var sales = depts.Single(d => d.Name == "Sales");
        var east = depts.Single(d => d.Name == "Sales / East");
        Assert.Null(sales.ParentId);
        Assert.Equal(sales.Id, east.ParentId);

        // Closure: self link (depth 0) + Sales→East (depth 1).
        var closure = await db.DepartmentClosure.IgnoreQueryFilters().Where(c => c.OrganizationId == org.Id).ToListAsync();
        Assert.Contains(closure, c => c.AncestorId == sales.Id && c.DescendantId == sales.Id && c.Depth == 0);
        Assert.Contains(closure, c => c.AncestorId == sales.Id && c.DescendantId == east.Id && c.Depth == 1);

        var positions = await db.Positions.IgnoreQueryFilters().Where(p => p.OrganizationId == org.Id).ToListAsync();
        Assert.Equal(2, positions.Count);

        // Branch-scoped accounts placed correctly.
        var lnd = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == $"lndmanager@{slug}.local");
        Assert.Equal(sales.Id, lnd.DepartmentId);
        Assert.Contains("LndManager", lnd.RoleCodes);
        Assert.True(lnd.MustChangePassword);

        var deptManager = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == $"deptmanager@{slug}.local");
        Assert.Equal(east.Id, deptManager.DepartmentId);
        Assert.Contains("DeptManager", deptManager.RoleCodes);

        // System roles catalog (global, org-null) present and not duplicated by the second run.
        Assert.Equal(7, await db.Roles.CountAsync(r => r.OrganizationId == null));
    }

    private async Task RunSeederAsync(IOptions<SeedOptions> options)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var seeder = new IdentitySeeder(
            sp.GetRequiredService<IOrganizationRepository>(),
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<IRoleRepository>(),
            sp.GetRequiredService<IDepartmentRepository>(),
            sp.GetRequiredService<IPositionRepository>(),
            sp.GetRequiredService<IPasswordHasher>(),
            sp.GetRequiredService<IIdGenerator>(),
            options,
            NullLogger<IdentitySeeder>.Instance);
        await seeder.SeedAsync();
    }
}
