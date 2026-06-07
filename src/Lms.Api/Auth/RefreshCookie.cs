namespace Lms.Api.Auth;

/// <summary>
/// Reads/writes/clears the refresh-token cookie. HttpOnly + Secure + SameSite=Lax, scoped to the
/// auth route prefix and with no Domain (origin-only). The same-origin Next.js proxy makes Lax a
/// strong CSRF posture; the Fetch-Metadata filter is the second layer.
/// </summary>
public static class RefreshCookie
{
    public const string Name = "lms_refresh";
    private const string Path = "/api/v1/auth";

    public static string? Read(HttpContext http) => http.Request.Cookies[Name];

    public static void Write(HttpContext http, string rawToken, DateTimeOffset expiresAt) =>
        http.Response.Cookies.Append(Name, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = Path,
            Expires = expiresAt,
            // No Domain: defaults to the exact origin (never shared across subdomains).
        });

    public static void Clear(HttpContext http) =>
        http.Response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = Path,
        });
}
