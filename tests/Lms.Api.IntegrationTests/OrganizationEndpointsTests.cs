using System.Net;
using System.Net.Http.Json;

namespace Lms.Api.IntegrationTests;

[Collection(nameof(IntegrationCollection))]
public sealed class OrganizationEndpointsTests(WebAppFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact] // Covers AE1
    public async Task Create_then_list_returns_created_organization_with_uuid()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/organizations", new { name = "Acme Corp", slug = "Acme-Corp-AE1" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<OrgResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.Equal("acme-corp-ae1", created.Slug);   // domain-normalized
        Assert.Equal("Active", created.Status);

        var list = await _client.GetFromJsonAsync<List<OrgResponse>>("/api/v1/organizations");
        Assert.NotNull(list);
        Assert.Contains(list!, o => o.Id == created.Id);
    }

    [Fact] // Covers AE2
    public async Task Create_with_blank_name_returns_422_problem_json()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/organizations", new { name = "", slug = "valid-slug" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_with_duplicate_slug_returns_409()
    {
        var first = await _client.PostAsJsonAsync("/api/v1/organizations", new { name = "Dup", slug = "dup-co" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/v1/organizations", new { name = "Dup 2", slug = "dup-co" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Health_endpoints_report_healthy_when_database_is_up()
    {
        var live = await _client.GetAsync("/health/live");
        var ready = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    private sealed record OrgResponse(Guid Id, string Name, string Slug, string Status, DateTimeOffset CreatedAt);
}
