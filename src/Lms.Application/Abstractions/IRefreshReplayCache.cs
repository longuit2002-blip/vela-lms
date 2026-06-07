using Lms.Application.Auth.Dtos;

namespace Lms.Application.Abstractions;

/// <summary>
/// Short-lived idempotency cache keyed by a presented refresh-token hash. Lets a benign retry or
/// concurrent tab that re-presents a just-rotated token receive the <b>same</b> already-issued pair
/// (within the grace window) instead of tripping reuse detection. Implemented in Infrastructure
/// (in-memory; single-instance for this slice). Never mints a divergent token.
/// </summary>
public interface IRefreshReplayCache
{
    bool TryGet(string presentedHash, out AuthTokens tokens);

    void Set(string presentedHash, AuthTokens tokens, TimeSpan ttl);
}
