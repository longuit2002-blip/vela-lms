using Lms.Application.Abstractions;
using Lms.Application.Auth.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace Lms.Infrastructure.Security;

/// <summary>
/// In-memory implementation of the refresh replay cache (single-instance; a distributed cache would
/// replace this for multi-instance deployments — deferred).
/// </summary>
public sealed class RefreshReplayCache(IMemoryCache cache) : IRefreshReplayCache
{
    private static string Key(string presentedHash) => $"refresh-replay:{presentedHash}";

    public bool TryGet(string presentedHash, out AuthTokens tokens)
        => cache.TryGetValue(Key(presentedHash), out tokens!);

    public void Set(string presentedHash, AuthTokens tokens, TimeSpan ttl)
        => cache.Set(Key(presentedHash), tokens, ttl);
}
