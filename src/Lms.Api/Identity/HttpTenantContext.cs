using System.Security.Claims;
using Lms.Application.Abstractions;

namespace Lms.Api.Identity;

/// <summary>
/// Resolves the current user and tenant from the request's <c>ClaimsPrincipal</c> (raw <c>sub</c>/<c>org</c>
/// claims — JwtBearer is configured with claim mapping off). Backs both <see cref="ICurrentUser"/> and
/// <see cref="ITenantContext"/> so endpoints, the DbContext, and the tenant interceptor share one source.
/// </summary>
public sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext, ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid OrganizationId =>
        Guid.TryParse(Principal?.FindFirstValue("org"), out var org) ? org : Guid.Empty;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    // FindAll, not FindFirstValue: with MapInboundClaims = false the JWT `roles` array materializes as
    // multiple separate "roles" claims, so the first-value form would collapse a multi-role user to
    // their first role and drop the rest of their permissions.
    public IReadOnlyCollection<string> RoleCodes =>
        Principal?.FindAll("roles").Select(c => c.Value).ToArray() ?? [];
}
