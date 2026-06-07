using Lms.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Lms.Infrastructure.Authorization;

/// <summary>
/// Resolves effective permissions from the cached system roles catalog. The catalog is small and
/// changes only via the seed, so it is cached; the TTL is a security bound — capped at the access-token
/// lifetime so a corrected/revoked permission <em>definition</em> cannot outlive the token it was
/// issued under. Scoped (reads via the scoped role repository) but the cache is the singleton
/// <see cref="IMemoryCache"/>, so the catalog is shared across requests.
/// </summary>
public sealed class PermissionResolver(IRoleRepository roles, IMemoryCache cache) : IPermissionResolver
{
    private const string CatalogCacheKey = "system-roles:code-to-permissions";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5); // <= 15-min access-token lifetime
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>(StringComparer.Ordinal);

    public async Task<IReadOnlySet<string>> ResolveAsync(
        Guid userId, IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken)
    {
        if (roleCodes.Count == 0)
            return Empty;

        var catalog = await GetCatalogAsync(cancellationToken);

        var granted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in roleCodes)
            if (catalog.TryGetValue(code, out var permissions))
                granted.UnionWith(permissions);

        return granted;
    }

    private async Task<IReadOnlyDictionary<string, string[]>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CatalogCacheKey, out IReadOnlyDictionary<string, string[]>? cached) && cached is not null)
            return cached;

        var systemRoles = await roles.GetSystemRolesAsync(cancellationToken);
        var map = systemRoles.ToDictionary(r => r.Code, r => r.Permissions.ToArray(), StringComparer.Ordinal);

        cache.Set(CatalogCacheKey, (IReadOnlyDictionary<string, string[]>)map, CacheTtl);
        return map;
    }
}
