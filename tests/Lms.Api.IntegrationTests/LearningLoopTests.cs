using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lms.Application.Abstractions;
using Lms.Domain.Organizations;
using Lms.Domain.Roles;
using Lms.Domain.Users;
using Lms.Infrastructure.Persistence;
using Lms.Infrastructure.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lms.Api.IntegrationTests;

/// <summary>
/// End-to-end proof of the first learning loop (U7): instructor authors a course, an L&amp;D manager
/// publishes and assigns it, and a learner watches and completes the lesson — under real auth,
/// tenancy, and RLS. Also proves the persona split (instructor lacks assignments.create), the
/// publish-with-no-lessons gate, ownership-scoped 404s, and cross-tenant isolation.
/// </summary>
[Collection(nameof(IntegrationCollection))]
public sealed class LearningLoopTests(WebAppFactory factory)
{
    private const string Password = "Authz-Pass-123!";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Full_loop_author_publish_assign_learn_completes_the_enrollment()
    {
        var s = await SeedScenarioAsync();

        // Instructor authors a course with a module and a video lesson.
        var courseId = (await CreatedJson<IdResponse>(s.InstructorToken, "/api/v1/courses",
            new { title = "Customer Service 101", slug = "cs-101" })).Id;
        var moduleId = (await CreatedJson<IdResponse>(s.InstructorToken, $"/api/v1/courses/{courseId}/modules",
            new { title = "Intro" })).Id;
        var lessonId = (await CreatedJson<IdResponse>(s.InstructorToken, $"/api/v1/courses/{courseId}/modules/{moduleId}/lessons",
            new { title = "Welcome", videoUrl = "https://cdn.example.com/welcome.mp4", durationSeconds = 120 })).Id;

        // L&D manager publishes and assigns to the learner.
        var pubId = (await CreatedJson<IdResponse>(s.LndToken, "/api/v1/publications",
            new { courseId, title = "CS 101 — Internal" })).Id;
        Assert.Equal(HttpStatusCode.OK, (await PostAsync($"/api/v1/publications/{pubId}/publish", s.LndToken, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync($"/api/v1/publications/{pubId}/assign", s.LndToken, new { userIds = new[] { s.LearnerUserId } })).StatusCode);

        // Learner sees the assigned course on the dashboard.
        var queue = await OkJson<List<EnrollmentSummaryResponse>>(s.LearnerToken, "/api/v1/me/enrollments");
        var enrollment = Assert.Single(queue);
        Assert.Equal("Customer Service 101", enrollment.CourseTitle);
        Assert.Equal(0, enrollment.ProgressPercent);

        // Learner opens detail and completes the only lesson → 100% / Completed.
        var detail = await OkJson<EnrolledCourseDetailResponse>(s.LearnerToken, $"/api/v1/enrollments/{enrollment.EnrollmentId}");
        Assert.False(detail.Modules[0].Lessons[0].Completed);

        var complete = await PostAsync($"/api/v1/enrollments/{enrollment.EnrollmentId}/lessons/{lessonId}/complete", s.LearnerToken, new { });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var result = await complete.Content.ReadFromJsonAsync<CompleteLessonResponse>();
        Assert.Equal(100, result!.ProgressPercent);
        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task Learner_cannot_author_and_instructor_cannot_assign()
    {
        var s = await SeedScenarioAsync();

        // Learner lacks courses.create.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await PostAsync("/api/v1/courses", s.LearnerToken, new { title = "X", slug = "x" })).StatusCode);

        // Instructor can author + publish but lacks assignments.create — the persona split.
        var courseId = (await CreatedJson<IdResponse>(s.InstructorToken, "/api/v1/courses", new { title = "C", slug = "c-" + Guid.NewGuid().ToString("N")[..6] })).Id;
        var moduleId = (await CreatedJson<IdResponse>(s.InstructorToken, $"/api/v1/courses/{courseId}/modules", new { title = "M" })).Id;
        await CreatedJson<IdResponse>(s.InstructorToken, $"/api/v1/courses/{courseId}/modules/{moduleId}/lessons",
            new { title = "L", videoUrl = "https://cdn.example.com/a.mp4", durationSeconds = 60 });
        var pubId = (await CreatedJson<IdResponse>(s.InstructorToken, "/api/v1/publications", new { courseId, title = "P" })).Id;
        Assert.Equal(HttpStatusCode.OK, (await PostAsync($"/api/v1/publications/{pubId}/publish", s.InstructorToken, new { })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await PostAsync($"/api/v1/publications/{pubId}/assign", s.InstructorToken, new { userIds = new[] { s.LearnerUserId } })).StatusCode);
    }

    [Fact]
    public async Task Publishing_a_course_with_no_lessons_is_422()
    {
        var s = await SeedScenarioAsync();

        var courseId = (await CreatedJson<IdResponse>(s.InstructorToken, "/api/v1/courses", new { title = "Empty", slug = "empty-" + Guid.NewGuid().ToString("N")[..6] })).Id;
        var pubId = (await CreatedJson<IdResponse>(s.LndToken, "/api/v1/publications", new { courseId, title = "P" })).Id;

        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await PostAsync($"/api/v1/publications/{pubId}/publish", s.LndToken, new { })).StatusCode);
    }

    [Fact]
    public async Task Assigning_a_draft_publication_is_409()
    {
        var s = await SeedScenarioAsync();

        var courseId = (await CreatedJson<IdResponse>(s.InstructorToken, "/api/v1/courses", new { title = "C", slug = "c-" + Guid.NewGuid().ToString("N")[..6] })).Id;
        var moduleId = (await CreatedJson<IdResponse>(s.InstructorToken, $"/api/v1/courses/{courseId}/modules", new { title = "M" })).Id;
        await CreatedJson<IdResponse>(s.InstructorToken, $"/api/v1/courses/{courseId}/modules/{moduleId}/lessons",
            new { title = "L", videoUrl = "https://cdn.example.com/a.mp4", durationSeconds = 60 });
        var pubId = (await CreatedJson<IdResponse>(s.LndToken, "/api/v1/publications", new { courseId, title = "P" })).Id; // not published

        Assert.Equal(HttpStatusCode.Conflict,
            (await PostAsync($"/api/v1/publications/{pubId}/assign", s.LndToken, new { userIds = new[] { s.LearnerUserId } })).StatusCode);
    }

    [Fact]
    public async Task Learner_cannot_complete_another_learners_enrollment()
    {
        var s = await SeedScenarioAsync();
        var enrollmentId = await AuthorPublishAssignAsync(s, s.LearnerUserId);

        // The second learner owns no such enrollment → 404 (ownership, not 403).
        var lessonId = (await OkJson<EnrolledCourseDetailResponse>(s.LearnerToken, $"/api/v1/enrollments/{enrollmentId}"))
            .Modules[0].Lessons[0].Id;
        var response = await PostAsync($"/api/v1/enrollments/{enrollmentId}/lessons/{lessonId}/complete", s.SecondLearnerToken, new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrollments_are_tenant_isolated_and_rls_enforced()
    {
        var a = await SeedScenarioAsync();
        var b = await SeedScenarioAsync();
        var bEnrollmentId = await AuthorPublishAssignAsync(b, b.LearnerUserId);

        // Cross-tenant IDOR: org-A learner fetching org-B's enrollment is a 404 (EF filter), not 403/200.
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/v1/enrollments/{bEnrollmentId}", a.LearnerToken)).StatusCode);

        // RLS proven via the subject role: org-B's enrollment row is invisible under org-A's session var.
        Assert.Equal(0, await CountRowsAsync(factory.AppRoleConnectionString, "enrollments", bEnrollmentId, tenant: a.OrgId));
        Assert.Equal(1, await CountRowsAsync(factory.AppRoleConnectionString, "enrollments", bEnrollmentId, tenant: b.OrgId));
        Assert.Equal(0, await CountRowsAsync(factory.AppRoleConnectionString, "enrollments", bEnrollmentId, tenant: null)); // fail closed

        // Every learning-loop root table actually has an RLS policy (a missed table would silently leak).
        var policied = await PoliciedTablesAsync(factory.ConnectionString);
        Assert.Contains("courses", policied);
        Assert.Contains("publications", policied);
        Assert.Contains("enrollments", policied);
    }

    // ===== fixture =====

    private sealed record Scenario(
        Guid OrgId, Guid LearnerUserId, string InstructorToken, string LndToken, string LearnerToken, string SecondLearnerToken);

    private async Task<Scenario> SeedScenarioAsync()
    {
        await EnsureSystemRolesAsync();

        var orgId = Guid.NewGuid();
        var slug = "loop-" + orgId.ToString("N")[..8];
        string instructorEmail = $"instructor-{slug}@vela.local",
            lndEmail = $"lnd-{slug}@vela.local",
            learnerEmail = $"learner-{slug}@vela.local",
            learner2Email = $"learner2-{slug}@vela.local";
        Guid learnerUserId;

        using (var scope = factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<AppDbContext>();
            var hasher = sp.GetRequiredService<IPasswordHasher>();
            var idGenerator = sp.GetRequiredService<IIdGenerator>();

            db.Organizations.Add(Organization.Create(orgId, $"Org {slug}", slug));
            await db.SaveChangesAsync();

            AddUser(db, hasher, idGenerator, orgId, instructorEmail, ["Instructor"]);
            AddUser(db, hasher, idGenerator, orgId, lndEmail, ["LndManager"]);
            learnerUserId = AddUser(db, hasher, idGenerator, orgId, learnerEmail, ["Learner"]);
            AddUser(db, hasher, idGenerator, orgId, learner2Email, ["Learner"]);
            await db.SaveChangesAsync();
        }

        return new Scenario(
            orgId, learnerUserId,
            await LoginAsync(instructorEmail), await LoginAsync(lndEmail),
            await LoginAsync(learnerEmail), await LoginAsync(learner2Email));
    }

    /// <summary>Runs the author→publish→assign chain and returns the learner's enrollment id.</summary>
    private async Task<Guid> AuthorPublishAssignAsync(Scenario s, Guid learnerUserId)
    {
        var courseId = (await CreatedJson<IdResponse>(s.InstructorToken, "/api/v1/courses", new { title = "C", slug = "c-" + Guid.NewGuid().ToString("N")[..6] })).Id;
        var moduleId = (await CreatedJson<IdResponse>(s.InstructorToken, $"/api/v1/courses/{courseId}/modules", new { title = "M" })).Id;
        await CreatedJson<IdResponse>(s.InstructorToken, $"/api/v1/courses/{courseId}/modules/{moduleId}/lessons",
            new { title = "L", videoUrl = "https://cdn.example.com/a.mp4", durationSeconds = 60 });
        var pubId = (await CreatedJson<IdResponse>(s.LndToken, "/api/v1/publications", new { courseId, title = "P" })).Id;
        await PostAsync($"/api/v1/publications/{pubId}/publish", s.LndToken, new { });
        await PostAsync($"/api/v1/publications/{pubId}/assign", s.LndToken, new { userIds = new[] { learnerUserId } });

        var queue = await OkJson<List<EnrollmentSummaryResponse>>(s.LearnerToken, "/api/v1/me/enrollments");
        return queue[0].EnrollmentId;
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

    private static Guid AddUser(AppDbContext db, IPasswordHasher hasher, IIdGenerator idGenerator, Guid orgId, string email, string[] roleCodes)
    {
        var user = User.Create(idGenerator.NewId(), orgId, email, hasher.Hash(Password), roleCodes, mustChangePassword: false);
        db.Users.Add(user);
        return user.Id;
    }

    private async Task<string> LoginAsync(string email)
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<AuthBody>();
        return body!.AccessToken;
    }

    private static async Task<int> CountRowsAsync(string connectionString, string table, Guid id, Guid? tenant)
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
        cmd.CommandText = $"SELECT count(*) FROM {table} WHERE id = @id";
        cmd.Parameters.AddWithValue("id", id);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<HashSet<string>> PoliciedTablesAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT tablename FROM pg_policies";
        var tables = new HashSet<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));
        return tables;
    }

    private async Task<T> CreatedJson<T>(string token, string url, object body)
    {
        var response = await PostAsync(url, token, body);
        if (response.StatusCode != HttpStatusCode.Created)
            throw new Xunit.Sdk.XunitException($"POST {url} -> {(int)response.StatusCode}\n{await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<T> OkJson<T>(string token, string url)
    {
        var response = await GetAsync(url, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private Task<HttpResponseMessage> GetAsync(string url, string token) => SendAsync(HttpMethod.Get, url, token, null);
    private Task<HttpResponseMessage> PostAsync(string url, string token, object body) => SendAsync(HttpMethod.Post, url, token, body);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private sealed record AuthBody(string AccessToken, DateTimeOffset AccessTokenExpiresAt, bool MustChangePassword);
    private sealed record IdResponse(Guid Id);
    private sealed record EnrollmentSummaryResponse(Guid EnrollmentId, Guid PublicationId, string CourseTitle, string CourseSlug, int ProgressPercent, string Status);
    private sealed record EnrolledCourseDetailResponse(Guid EnrollmentId, string CourseTitle, string Status, int ProgressPercent, List<EnrolledModuleResponse> Modules);
    private sealed record EnrolledModuleResponse(Guid Id, string Title, int Order, List<EnrolledLessonResponse> Lessons);
    private sealed record EnrolledLessonResponse(Guid Id, string Title, int Order, string VideoUrl, int DurationSeconds, bool Completed);
    private sealed record CompleteLessonResponse(Guid EnrollmentId, Guid LessonId, int ProgressPercent, string Status);
}
