namespace Lms.Api.Auth;

/// <summary>
/// CSRF second layer (after SameSite=Lax): rejects a mutating auth request only when the browser
/// explicitly marks it cross-site via <c>Sec-Fetch-Site: cross-site</c>. Absent header (e.g. the
/// same-origin Next.js server-side proxy call) or same-origin/same-site/none is allowed, so the BFF
/// proxy is not blocked. A signed double-submit token is deferred (see plan).
/// </summary>
public sealed class FetchMetadataEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var site = context.HttpContext.Request.Headers["Sec-Fetch-Site"].ToString();
        if (string.Equals(site, "cross-site", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Cross-site request rejected",
                type: "https://errors.vela.app/csrf");
        }

        return await next(context);
    }
}
