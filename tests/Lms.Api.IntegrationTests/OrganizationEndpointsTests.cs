using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lms.Application.Abstractions;
using Lms.Domain.Organizations;
using Lms.Domain.Users;
using Lms.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Api.IntegrationTests;

[Collection(nameof(IntegrationCollection))]
public sealed class OrganizationEndpointsTests(WebAppFactory factory)
{
    private const string Password = "Test-Password-123!";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact] // Covers AE6 — authenticated caller sees only their own organization
    public async Task My_organization_returns_the_callers_own_org()
    {
        var (slug, accessToken) = await SeedAndLoginAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organizations/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var org = await response.Content.ReadFromJsonAsync<OrgResponse>();
        Assert.NotNull(org);
        Assert.Equal(slug, org!.Slug);
    }

    [Fact]
    public async Task My_organization_requires_authentication()
    {
        var response = await _client.GetAsync("/api/v1/organizations/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cross_tenant_list_and_create_endpoints_are_retired()
    {
        var list = await _client.GetAsync("/api/v1/organizations");
        var create = await _client.PostAsJsonAsync("/api/v1/organizations", new { name = "Nope", slug = "nope" });

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
    }

    [Fact]
    public async Task Health_endpoints_report_healthy_when_database_is_up()
    {
        var live = await _client.GetAsync("/health/live");
        var ready = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    private async Task<(string Slug, string AccessToken)> SeedAndLoginAsync()
    {
        string slug, email;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var orgId = Guid.NewGuid();
            slug = "org-" + orgId.ToString("N")[..10];
            email = $"{slug}@vela.local";
            db.Organizations.Add(Organization.Create(orgId, $"Org {slug}", slug));
            db.Users.Add(User.Create(Guid.NewGuid(), orgId, email, hasher.Hash(Password), ["OrgOwner"], false));
            await db.SaveChangesAsync();
        }

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<AuthBody>();
        return (slug, body!.AccessToken);
    }

    private sealed record OrgResponse(Guid Id, string Name, string Slug, string Status, DateTimeOffset CreatedAt);

    private sealed record AuthBody(string AccessToken, DateTimeOffset AccessTokenExpiresAt, bool MustChangePassword);
}
