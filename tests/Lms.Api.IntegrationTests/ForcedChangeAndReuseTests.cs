using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lms.Application.Abstractions;
using Lms.Domain.Organizations;
using Lms.Domain.Users;
using Lms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Api.IntegrationTests;

[Collection(nameof(IntegrationCollection))]
public sealed class ForcedChangeAndReuseTests(WebAppFactory factory)
{
    private const string Password = "Test-Password-123!";

    [Fact] // Covers F2 / AE3 — forced first-login change gate
    public async Task Must_change_user_is_gated_until_password_changed()
    {
        var client = factory.CreateClient();
        var email = await SeedUserAsync(factory, mustChange: true);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        var body = await login.Content.ReadFromJsonAsync<AuthBody>();
        Assert.True(body!.MustChangePassword);

        // Gated: any protected non-auth endpoint is blocked until the change.
        var blocked = await Get(client, "/api/v1/organizations/me", body.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Contains("must-change-password", await blocked.Content.ReadAsStringAsync());

        // Change the password → fresh token without the gate claim.
        var change = await Post(client, "/api/v1/auth/change-password", body.AccessToken,
            new { currentPassword = Password, newPassword = "Brand-New-Pass-456!" });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        var changed = await change.Content.ReadFromJsonAsync<AuthBody>();
        Assert.False(changed!.MustChangePassword);

        // Now the new token passes the gate.
        var allowed = await Get(client, "/api/v1/organizations/me", changed.AccessToken);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact] // Covers AE2 — reuse outside the grace window revokes the family
    public async Task Reusing_a_rotated_token_outside_grace_revokes_the_family()
    {
        // A host with the grace window disabled so a re-presented rotated token is treated as reuse.
        using var noGrace = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:RefreshGraceSeconds"] = "0" })));
        var client = noGrace.CreateClient();
        var email = await SeedUserAsync(noGrace, mustChange: false);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        var original = ExtractRefreshCookie(login)!;

        var refresh = await PostWithCookie(client, "/api/v1/auth/refresh", original);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var child = ExtractRefreshCookie(refresh)!;

        // Re-presenting the already-rotated original (no grace) → reuse detected.
        var reuse = await PostWithCookie(client, "/api/v1/auth/refresh", original);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // The whole family is revoked — even the legitimate child no longer works.
        var childAfter = await PostWithCookie(client, "/api/v1/auth/refresh", child);
        Assert.Equal(HttpStatusCode.Unauthorized, childAfter.StatusCode);
    }

    private static async Task<string> SeedUserAsync(WebApplicationFactory<Program> factoryLike, bool mustChange)
    {
        using var scope = factoryLike.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var orgId = Guid.NewGuid();
        var slug = "fc-" + orgId.ToString("N")[..10];
        var email = $"{slug}@vela.local";
        db.Organizations.Add(Organization.Create(orgId, $"Org {slug}", slug));
        db.Users.Add(User.Create(Guid.NewGuid(), orgId, email, hasher.Hash(Password), ["OrgOwner"], mustChange));
        await db.SaveChangesAsync();
        return email;
    }

    private static Task<HttpResponseMessage> Get(HttpClient client, string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string url, string accessToken, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> PostWithCookie(HttpClient client, string url, string refreshCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(string.Empty) };
        request.Headers.Add("Cookie", $"lms_refresh={refreshCookie}");
        return client.SendAsync(request);
    }

    private static string? ExtractRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;
        foreach (var cookie in cookies)
        {
            if (cookie.StartsWith("lms_refresh=", StringComparison.Ordinal))
            {
                var value = cookie["lms_refresh=".Length..];
                var semicolon = value.IndexOf(';');
                return semicolon >= 0 ? value[..semicolon] : value;
            }
        }
        return null;
    }

    private sealed record AuthBody(string AccessToken, DateTimeOffset AccessTokenExpiresAt, bool MustChangePassword);
}
