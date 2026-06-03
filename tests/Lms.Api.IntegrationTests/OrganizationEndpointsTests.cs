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
        Assert.NotNull(create.Headers.Location);   // 201 carries a Location header
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
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"name\"", body);   // field key is camelCase, not "Name"
    }

    [Theory] // Malformed-but-plausible slugs are 422 (validator mirrors the domain grammar), not 500.
    [InlineData("a--b")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    public async Task Create_with_malformed_slug_returns_422(string slug)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/organizations", new { name = "Acme", slug });

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
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
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
