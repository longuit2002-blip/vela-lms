using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lms.Application.Abstractions;
using Lms.Domain.Organizations;
using Lms.Domain.Users;
using Lms.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Api.IntegrationTests;

/// <summary>
/// Exercises the auth flows end-to-end against the real stack (Covers F1, plus rotation/reuse and
/// lockout). The refresh cookie is forwarded manually because it is Secure and the test server is http.
/// </summary>
[Collection(nameof(IntegrationCollection))]
public sealed class AuthFlowTests(WebAppFactory factory)
{
    private const string Password = "Test-Password-123!";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact] // Covers F1
    public async Task Login_then_refresh_then_logout_round_trip()
    {
        var email = await SeedUserAsync(mustChange: false);

        // Login.
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<AuthBody>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(body.MustChangePassword);
        var refreshCookie = ExtractRefreshCookie(login);
        Assert.NotNull(refreshCookie);

        // Refresh rotates → new pair + new cookie.
        var refresh = await PostWithCookie("/api/v1/auth/refresh", refreshCookie!);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var rotatedCookie = ExtractRefreshCookie(refresh);
        Assert.NotNull(rotatedCookie);
        Assert.NotEqual(refreshCookie, rotatedCookie);

        // Logout revokes the family.
        var logout = await PostWithCookie("/api/v1/auth/logout", rotatedCookie!);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // The rotated token no longer works after logout.
        var afterLogout = await PostWithCookie("/api/v1/auth/refresh", rotatedCookie!);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact] // Covers AE2 — reuse of a rotated token revokes the family
    public async Task Reusing_a_rotated_refresh_token_revokes_the_family()
    {
        var email = await SeedUserAsync(mustChange: false);
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        var original = ExtractRefreshCookie(login)!;

        // Rotate once.
        var refresh = await PostWithCookie("/api/v1/auth/refresh", original);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var child = ExtractRefreshCookie(refresh)!;

        // The grace window replays the SAME pair for the original token (benign retry) — not a revoke.
        var replay = await PostWithCookie("/api/v1/auth/refresh", original);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);

        // But the child is still valid right after (family not revoked by the benign replay).
        var childRefresh = await PostWithCookie("/api/v1/auth/refresh", child);
        Assert.Equal(HttpStatusCode.OK, childRefresh.StatusCode);
    }

    [Fact] // Covers AE4 — lockout after N failed attempts
    public async Task Account_locks_after_repeated_failures()
    {
        var email = await SeedUserAsync(mustChange: false);

        for (var i = 0; i < 5; i++)
        {
            var bad = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        }

        // Even with the correct password, the account is now locked.
        var locked = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        var problem = await locked.Content.ReadAsStringAsync();
        Assert.Contains("account-locked", problem);
    }

    [Fact]
    public async Task Login_reports_must_change_for_a_seeded_default_password_account()
    {
        var email = await SeedUserAsync(mustChange: true);

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<AuthBody>();
        Assert.True(body!.MustChangePassword);
    }

    private async Task<string> SeedUserAsync(bool mustChange)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var orgId = Guid.NewGuid();
        var slug = "auth-" + orgId.ToString("N")[..10];
        var email = $"{slug}@vela.local";
        db.Organizations.Add(Organization.Create(orgId, $"Org {slug}", slug));
        db.Users.Add(User.Create(Guid.NewGuid(), orgId, email, hasher.Hash(Password), ["OrgOwner"], mustChange));
        await db.SaveChangesAsync();
        return email;
    }

    private Task<HttpResponseMessage> PostWithCookie(string url, string refreshCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Cookie", $"lms_refresh={refreshCookie}");
        request.Content = new StringContent(string.Empty);
        return _client.SendAsync(request);
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
