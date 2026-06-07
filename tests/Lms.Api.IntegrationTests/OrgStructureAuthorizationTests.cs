using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lms.Application.Abstractions;
using Lms.Domain.Departments;
using Lms.Domain.Organizations;
using Lms.Domain.Roles;
using Lms.Domain.Users;
using Lms.Infrastructure.Persistence;
using Lms.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lms.Api.IntegrationTests;

/// <summary>
/// End-to-end proof of the authorization spine (U10): RBAC, dept-branch ABAC (incl. reparent),
/// closure integrity, delete-block, positions, pipeline ordering, multi-role union, and tenant
/// isolation of the new tables. Each test seeds its own org + tree + placed users (mustChangePassword
/// = false so they can act immediately) and logs in as each persona.
/// </summary>
[Collection(nameof(IntegrationCollection))]
public sealed class OrgStructureAuthorizationTests(WebAppFactory factory)
{
    private const string Password = "Authz-Pass-123!";
    private readonly HttpClient _client = factory.CreateClient();

    // ----- AE1: RBAC -----

    [Fact] // Covers AE1, R16
    public async Task Tree_management_is_denied_to_learner_and_allowed_to_owner()
    {
        var s = await SeedScenarioAsync();

        var learner = await PostAsync("/api/v1/departments", s.LearnerToken, new { name = "Nope", parentId = (Guid?)null });
        Assert.Equal(HttpStatusCode.Forbidden, learner.StatusCode);

        var owner = await PostAsync("/api/v1/departments", s.OwnerToken, new { name = "New Root", parentId = (Guid?)null });
        Assert.Equal(HttpStatusCode.Created, owner.StatusCode);
    }

    [Fact] // Covers R16 — DeptManager has no tree-management permission (its ◐ is over users)
    public async Task Tree_management_is_denied_to_dept_manager()
    {
        var s = await SeedScenarioAsync();

        var response = await PostAsync("/api/v1/departments", s.DeptManagerToken, new { name = "Nope", parentId = (Guid?)null });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ----- AE2: dept-branch ABAC -----

    [Fact] // Covers AE2, R17
    public async Task Branch_manager_confined_to_subtree_full_scope_unrestricted()
    {
        var s = await SeedScenarioAsync();

        // LndManager placed in Sales: in-branch rename succeeds, out-of-branch is forbidden.
        var inBranch = await PatchAsync($"/api/v1/departments/{s.SalesEast}", s.LndManagerToken, new { name = "East Renamed" });
        Assert.Equal(HttpStatusCode.OK, inBranch.StatusCode);

        var outOfBranch = await PatchAsync($"/api/v1/departments/{s.Engineering}", s.LndManagerToken, new { name = "Eng Renamed" });
        Assert.Equal(HttpStatusCode.Forbidden, outOfBranch.StatusCode);

        // OrgOwner (full-scope) manages anywhere.
        var fullScope = await PatchAsync($"/api/v1/departments/{s.Engineering}", s.OwnerToken, new { name = "Engineering OK" });
        Assert.Equal(HttpStatusCode.OK, fullScope.StatusCode);
    }

    [Fact] // Reparent escalation — guard must check BOTH the moved node and the new parent
    public async Task Branch_manager_cannot_relocate_subtree_out_of_branch()
    {
        var s = await SeedScenarioAsync();

        // Sales/East is in the LndManager's branch, but Engineering is not — moving East under
        // Engineering would push it out of the manager's scope, so it must be forbidden.
        var move = await PostAsync($"/api/v1/departments/{s.SalesEast}/move", s.LndManagerToken, new { newParentId = (Guid?)s.Engineering });
        Assert.Equal(HttpStatusCode.Forbidden, move.StatusCode);
    }

    // ----- AE3: closure integrity -----

    [Fact] // Covers AE3, R18
    public async Task Reparent_updates_closure_and_rejects_cycles()
    {
        var s = await SeedScenarioAsync();

        // Move Sales/East under Engineering — succeeds and the parent changes.
        var move = await PostAsync($"/api/v1/departments/{s.SalesEast}/move", s.OwnerToken, new { newParentId = (Guid?)s.Engineering });
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);
        var moved = await move.Content.ReadFromJsonAsync<DeptResponse>();
        Assert.Equal(s.Engineering, moved!.ParentId);

        // Cycle: moving Sales under Sales/West (its own descendant) is rejected.
        var cycle = await PostAsync($"/api/v1/departments/{s.Sales}/move", s.OwnerToken, new { newParentId = (Guid?)s.SalesWest });
        Assert.Equal(HttpStatusCode.Conflict, cycle.StatusCode);
    }

    // ----- AE4: delete-block -----

    [Fact] // Covers AE4, R18
    public async Task Delete_is_blocked_for_nonempty_and_allowed_for_empty_leaf()
    {
        var s = await SeedScenarioAsync();

        // Sales has children → blocked.
        Assert.Equal(HttpStatusCode.Conflict, (await DeleteAsync($"/api/v1/departments/{s.Sales}", s.OwnerToken)).StatusCode);
        // Sales/East has the DeptManager assigned → blocked.
        Assert.Equal(HttpStatusCode.Conflict, (await DeleteAsync($"/api/v1/departments/{s.SalesEast}", s.OwnerToken)).StatusCode);
        // Engineering is an empty leaf → deleted.
        Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/v1/departments/{s.Engineering}", s.OwnerToken)).StatusCode);
    }

    // ----- AE5: positions -----

    [Fact] // Covers AE5
    public async Task Positions_enforce_unique_name_and_block_delete_when_held()
    {
        var s = await SeedScenarioAsync();

        var created = await PostAsync("/api/v1/positions", s.OwnerToken, new { name = "Supervisor" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var positionId = (await created.Content.ReadFromJsonAsync<PositionResponse>())!.Id;

        // Duplicate name → conflict.
        Assert.Equal(HttpStatusCode.Conflict, (await PostAsync("/api/v1/positions", s.OwnerToken, new { name = "Supervisor" })).StatusCode);

        // Assign a user to the position, then delete is blocked.
        await AssignPositionAsync(s.LearnerUserId, positionId);
        Assert.Equal(HttpStatusCode.Conflict, (await DeleteAsync($"/api/v1/positions/{positionId}", s.OwnerToken)).StatusCode);

        // An unused position deletes cleanly.
        var unused = await PostAsync("/api/v1/positions", s.OwnerToken, new { name = "Analyst" });
        var unusedId = (await unused.Content.ReadFromJsonAsync<PositionResponse>())!.Id;
        Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/v1/positions/{unusedId}", s.OwnerToken)).StatusCode);
    }

    // ----- pipeline ordering -----

    [Fact] // Authorize-before-validate: a request that is both unauthorized AND invalid returns 403, not 422
    public async Task Authorization_runs_before_validation_and_403_is_problem_json()
    {
        var s = await SeedScenarioAsync();

        // Learner lacks departments.manage AND the body is invalid (empty name).
        var response = await PostAsync("/api/v1/departments", s.LearnerToken, new { name = "", parentId = (Guid?)null });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // 403, not 422 (and not 500)
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // ----- multi-role union -----

    [Fact] // A user's permissions are the union of all their roles
    public async Task Multi_role_user_gets_the_union_of_permissions()
    {
        var s = await SeedScenarioAsync();

        // Learner alone cannot manage the tree; Learner + OrgOwner can (union includes manage.all).
        var email = $"multi-{Guid.NewGuid():N}@vela.local";
        await AddUserAsync(s.OrgId, email, ["Learner", "OrgOwner"], departmentId: null);
        var token = await LoginAsync(email);

        var response = await PostAsync("/api/v1/departments", token, new { name = "By Multi-Role", parentId = (Guid?)null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ----- AE7: tenant isolation + cross-tenant IDOR -----

    [Fact] // Covers AE7, R19
    public async Task Departments_are_tenant_isolated_across_orgs_and_rls()
    {
        var a = await SeedScenarioAsync();
        var b = await SeedScenarioAsync();

        // Org-A owner lists only org-A departments.
        var list = await GetAsync("/api/v1/departments", a.OwnerToken);
        var depts = await list.Content.ReadFromJsonAsync<List<DeptResponse>>();
        Assert.Contains(depts!, d => d.Id == a.Sales);
        Assert.DoesNotContain(depts!, d => d.Id == b.Sales);

        // Cross-tenant IDOR: fetching org-B's department in an org-A session is a 404 (EF filter), not 403/200.
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/v1/departments/{b.Sales}", a.OwnerToken)).StatusCode);

        // RLS proven independently via the subject role: org-B's row is invisible under org-A's session var.
        Assert.Equal(0, await CountDepartmentAsync(factory.AppRoleConnectionString, b.Sales, tenant: a.OrgId));
        Assert.Equal(1, await CountDepartmentAsync(factory.AppRoleConnectionString, b.Sales, tenant: b.OrgId));
        Assert.Equal(0, await CountDepartmentAsync(factory.AppRoleConnectionString, b.Sales, tenant: null)); // fail closed
    }

    // ===== fixture =====

    private sealed record Scenario(
        Guid OrgId, Guid Sales, Guid SalesEast, Guid SalesWest, Guid Engineering, Guid LearnerUserId,
        string OwnerToken, string LndManagerToken, string DeptManagerToken, string LearnerToken);

    private async Task<Scenario> SeedScenarioAsync()
    {
        await EnsureSystemRolesAsync();

        var orgId = Guid.NewGuid();
        var slug = "authz-" + orgId.ToString("N")[..8];
        Guid sales, salesEast, salesWest, engineering, learnerUserId;
        string ownerEmail = $"owner-{slug}@vela.local",
            lndEmail = $"lnd-{slug}@vela.local",
            deptEmail = $"dept-{slug}@vela.local",
            learnerEmail = $"learner-{slug}@vela.local";

        using (var scope = factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<AppDbContext>();
            var departments = sp.GetRequiredService<IDepartmentRepository>();
            var idGenerator = sp.GetRequiredService<IIdGenerator>();
            var hasher = sp.GetRequiredService<IPasswordHasher>();

            db.Organizations.Add(Organization.Create(orgId, $"Org {slug}", slug));
            await db.SaveChangesAsync();

            sales = await AddDepartmentAsync(departments, idGenerator, orgId, null, "Sales");
            salesWest = await AddDepartmentAsync(departments, idGenerator, orgId, sales, "Sales / West");
            salesEast = await AddDepartmentAsync(departments, idGenerator, orgId, sales, "Sales / East");
            engineering = await AddDepartmentAsync(departments, idGenerator, orgId, null, "Engineering");

            AddUser(db, hasher, idGenerator, orgId, ownerEmail, ["OrgOwner"], null);
            AddUser(db, hasher, idGenerator, orgId, lndEmail, ["LndManager"], sales);
            AddUser(db, hasher, idGenerator, orgId, deptEmail, ["DeptManager"], salesEast);
            learnerUserId = AddUser(db, hasher, idGenerator, orgId, learnerEmail, ["Learner"], null);
            await db.SaveChangesAsync();
        }

        return new Scenario(
            orgId, sales, salesEast, salesWest, engineering, learnerUserId,
            await LoginAsync(ownerEmail), await LoginAsync(lndEmail), await LoginAsync(deptEmail), await LoginAsync(learnerEmail));
    }

    private async Task EnsureSystemRolesAsync()
    {
        using var scope = factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var idGenerator = scope.ServiceProvider.GetRequiredService<IIdGenerator>();
        var added = false;
        foreach (var role in SystemRoles.All)
        {
            if (await roles.SystemRoleExistsAsync(role.Code, CancellationToken.None))
                continue;
            await roles.AddAsync(Role.CreateSystem(idGenerator.NewId(), role.Code, role.Name, role.Permissions), CancellationToken.None);
            added = true;
        }
        if (added)
            await roles.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<Guid> AddDepartmentAsync(
        IDepartmentRepository departments, IIdGenerator idGenerator, Guid orgId, Guid? parentId, string name)
    {
        var department = Department.Create(idGenerator.NewId(), orgId, parentId, name);
        await departments.AddAsync(department, CancellationToken.None);
        await departments.SaveChangesAsync(CancellationToken.None); // commit so the next child's closure read sees it
        return department.Id;
    }

    private static Guid AddUser(
        AppDbContext db, IPasswordHasher hasher, IIdGenerator idGenerator,
        Guid orgId, string email, string[] roleCodes, Guid? departmentId)
    {
        var user = User.Create(idGenerator.NewId(), orgId, email, hasher.Hash(Password), roleCodes, mustChangePassword: false);
        if (departmentId is { } dept)
            user.PlaceIn(dept, positionId: null, DateTimeOffset.UtcNow);
        db.Users.Add(user);
        return user.Id;
    }

    private async Task AddUserAsync(Guid orgId, string email, string[] roleCodes, Guid? departmentId)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        AddUser(sp.GetRequiredService<AppDbContext>(), sp.GetRequiredService<IPasswordHasher>(),
            sp.GetRequiredService<IIdGenerator>(), orgId, email, roleCodes, departmentId);
        await sp.GetRequiredService<AppDbContext>().SaveChangesAsync();
    }

    private async Task AssignPositionAsync(Guid userId, Guid positionId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        user.PlaceIn(user.DepartmentId, positionId, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    private async Task<string> LoginAsync(string email)
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<AuthBody>();
        return body!.AccessToken;
    }

    private static async Task<int> CountDepartmentAsync(string connectionString, Guid departmentId, Guid? tenant)
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
        cmd.CommandText = "SELECT count(*) FROM departments WHERE id = @id";
        cmd.Parameters.AddWithValue("id", departmentId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private Task<HttpResponseMessage> GetAsync(string url, string token) => SendAsync(HttpMethod.Get, url, token, null);
    private Task<HttpResponseMessage> PostAsync(string url, string token, object body) => SendAsync(HttpMethod.Post, url, token, body);
    private Task<HttpResponseMessage> PatchAsync(string url, string token, object body) => SendAsync(HttpMethod.Patch, url, token, body);
    private Task<HttpResponseMessage> DeleteAsync(string url, string token) => SendAsync(HttpMethod.Delete, url, token, null);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private sealed record AuthBody(string AccessToken, DateTimeOffset AccessTokenExpiresAt, bool MustChangePassword);
    private sealed record DeptResponse(Guid Id, Guid? ParentId, string Name, DateTimeOffset CreatedAt);
    private sealed record PositionResponse(Guid Id, string Name, DateTimeOffset CreatedAt);
}
