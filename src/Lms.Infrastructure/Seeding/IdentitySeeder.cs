using Lms.Application.Abstractions;
using Lms.Application.Auth;
using Lms.Domain.Departments;
using Lms.Domain.Organizations;
using Lms.Domain.Positions;
using Lms.Domain.Roles;
using Lms.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lms.Infrastructure.Seeding;

/// <summary>
/// Dev seeder (the only provisioning path). Idempotent throughout: ensures the system roles catalog,
/// the first Organization + its OrgOwner, and a demo department tree with branch-scoped accounts
/// (an LndManager in a branch, a DeptManager and a Learner) so the authorization spine is runnable
/// and demo-able end to end. All seeded accounts get <c>mustChangePassword = true</c> so a seeded
/// default password can't be used past first login. Runs as the app's DB connection (superuser,
/// RLS-bypassing) with no HTTP/tenant context — so closure rows are written with their
/// <c>organization_id</c> stamped explicitly by the repository, not inferred from a tenant filter.
/// </summary>
public sealed class IdentitySeeder(
    IOrganizationRepository organizations,
    IUserRepository users,
    IRoleRepository roles,
    IDepartmentRepository departments,
    IPositionRepository positions,
    IPasswordHasher passwordHasher,
    IIdGenerator idGenerator,
    IOptions<SeedOptions> options,
    ILogger<IdentitySeeder> logger)
{
    public const string OrgOwnerRole = "OrgOwner";

    private static readonly string[] DemoPositions = ["Manager", "Agent"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seed = options.Value;
        if (!seed.Enabled)
            return;

        // System roles are the authorization catalog — ensure them first (idempotent). (Production role
        // provisioning is a deployment concern deferred with the rest of deployment; this dev seeder is
        // the only seed path today.)
        await SeedSystemRolesAsync(cancellationToken);

        var organization = await organizations.FindBySlugAsync(seed.OrganizationSlug, cancellationToken);
        if (organization is null)
        {
            organization = Organization.Create(idGenerator.NewId(), seed.OrganizationName, seed.OrganizationSlug);
            await organizations.AddAsync(organization, cancellationToken);
            await organizations.SaveChangesAsync(cancellationToken);
        }

        if (await users.FindByEmailForLoginAsync(seed.OwnerEmail, cancellationToken) is null)
        {
            var owner = User.Create(
                idGenerator.NewId(), organization.Id, seed.OwnerEmail,
                passwordHasher.Hash(seed.OwnerPassword), [OrgOwnerRole], mustChangePassword: true);
            await users.AddAsync(owner, cancellationToken);
            await users.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Identity seed created organization {Slug} and OrgOwner {Email}.", organization.Slug, owner.Email);
        }

        await SeedDemoStructureAsync(organization, seed, cancellationToken);
    }

    /// <summary>Idempotently provisions the system roles catalog (org-wide, organization_id null).</summary>
    private async Task SeedSystemRolesAsync(CancellationToken cancellationToken)
    {
        var added = 0;
        foreach (var role in SystemRoles.All)
        {
            if (await roles.SystemRoleExistsAsync(role.Code, cancellationToken))
                continue;

            await roles.AddAsync(
                Role.CreateSystem(idGenerator.NewId(), role.Code, role.Name, role.Permissions),
                cancellationToken);
            added++;
        }

        if (added > 0)
        {
            await roles.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Identity seed ensured {Added} system role(s).", added);
        }
    }

    /// <summary>
    /// Provisions a demo department tree (Sales → {Sales/East, Sales/West}, Engineering), the demo
    /// positions, and three branch-scoped accounts. Idempotent via the LndManager's existence.
    /// Departments are saved per node so each child's closure read sees its committed parent.
    /// </summary>
    private async Task SeedDemoStructureAsync(Organization organization, SeedOptions seed, CancellationToken cancellationToken)
    {
        var lndEmail = DemoEmail("lndmanager", organization.Slug);
        if (await users.FindByEmailForLoginAsync(lndEmail, cancellationToken) is not null)
            return; // structure already seeded

        var now = DateTimeOffset.UtcNow;

        var sales = await AddDepartmentAsync(null, "Sales");
        await AddDepartmentAsync(null, "Engineering");
        var east = await AddDepartmentAsync(sales.Id, "Sales / East");
        await AddDepartmentAsync(sales.Id, "Sales / West");

        foreach (var name in DemoPositions)
            await positions.AddAsync(Position.Create(idGenerator.NewId(), organization.Id, name), cancellationToken);
        await positions.SaveChangesAsync(cancellationToken);

        // LndManager has branch-limited org-tree management (ABAC-positive subject, placed in Sales);
        // DeptManager has no tree-management permission (RBAC-negative); Learner has none either.
        await AddPlacedUserAsync(lndEmail, "LndManager", sales.Id);
        await AddPlacedUserAsync(DemoEmail("deptmanager", organization.Slug), "DeptManager", east.Id);
        await AddPlacedUserAsync(DemoEmail("learner", organization.Slug), "Learner", east.Id);
        await users.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Identity seed provisioned the demo department tree + branch-scoped accounts for {Slug}.", organization.Slug);

        async Task<Department> AddDepartmentAsync(Guid? parentId, string name)
        {
            var department = Department.Create(idGenerator.NewId(), organization.Id, parentId, name);
            await departments.AddAsync(department, cancellationToken);
            await departments.SaveChangesAsync(cancellationToken); // commit so a child's closure read sees the parent
            return department;
        }

        async Task AddPlacedUserAsync(string email, string roleCode, Guid departmentId)
        {
            var user = User.Create(
                idGenerator.NewId(), organization.Id, email,
                passwordHasher.Hash(seed.OwnerPassword), [roleCode], mustChangePassword: true);
            user.PlaceIn(departmentId, positionId: null, now);
            await users.AddAsync(user, cancellationToken);
        }
    }

    private static string DemoEmail(string localPart, string slug) => $"{localPart}@{slug}.local";
}
