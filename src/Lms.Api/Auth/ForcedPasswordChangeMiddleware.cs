namespace Lms.Api.Auth;

/// <summary>
/// Enforces the forced first-login password change. When an authenticated request carries the
/// <c>mcp</c> (must-change-password) claim, every endpoint except the auth routes (so the user can
/// reach change-password and logout) is blocked with a problem response until the password is changed.
/// </summary>
public sealed class ForcedPasswordChangeMiddleware(RequestDelegate next)
{
    private const string AuthPathPrefix = "/api/v1/auth";

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        var mustChange = user.Identity?.IsAuthenticated == true
            && string.Equals(user.FindFirst("mcp")?.Value, "true", StringComparison.OrdinalIgnoreCase);

        if (mustChange && !context.Request.Path.StartsWithSegments(AuthPathPrefix))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://errors.vela.app/must-change-password",
                title = "Password change required",
                status = StatusCodes.Status403Forbidden,
                detail = "You must change your password before continuing.",
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
