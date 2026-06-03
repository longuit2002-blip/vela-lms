using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Auth.Dtos;
using Lms.Domain.Identity;
using Mediator;
using Microsoft.Extensions.Options;

namespace Lms.Application.Auth.Commands.Refresh;

/// <summary>
/// Rotates a refresh token. The flow, in order:
/// 1. Replay cache hit (benign retry / concurrent tab within the grace window) → return the SAME pair.
/// 2. Atomic consume (guarded UPDATE...RETURNING) → if won, mint a child + access token, cache the pair.
/// 3. Consume returned nothing → the token was already used/revoked and it isn't a cached replay →
///    treat as reuse: revoke the whole family and reject.
/// Roles are re-sourced from the user row on every rotation (no stale claims on a 14-day refresh).
/// </summary>
public sealed class RefreshTokenHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IRefreshTokenHasher refreshHasher,
    IJwtTokenIssuer tokenIssuer,
    IIdGenerator idGenerator,
    IRefreshReplayCache replayCache,
    IOptions<JwtOptions> jwt)
    : IRequestHandler<RefreshTokenCommand, Result<AuthTokens>>
{
    public async ValueTask<Result<AuthTokens>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var graceWindow = TimeSpan.FromSeconds(jwt.Value.RefreshGraceSeconds);

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            return Result.Unauthorized();

        var presentedHash = refreshHasher.Hash(command.RefreshToken);

        // (1) Benign retry / concurrent tab — return the identical pair already minted for this token.
        if (graceWindow > TimeSpan.Zero && replayCache.TryGet(presentedHash, out var cached))
            return Result.Success(cached);

        // (2) Atomic consume: only the winner of the race gets the row back.
        var childId = idGenerator.NewId();
        var consumed = await refreshTokens.TryConsumeAsync(presentedHash, childId, now, cancellationToken);

        if (consumed is null)
        {
            // (3) Already used/revoked and not a cached replay → reuse. Revoke the family (if known).
            var existing = await refreshTokens.FindByHashAsync(presentedHash, cancellationToken);
            if (existing is not null)
                await refreshTokens.RevokeFamilyAsync(existing.FamilyId, "reuse_detected", now, cancellationToken);
            return Result.Unauthorized();
        }

        if (now >= consumed.ExpiresAt)
            return Result.Unauthorized();

        var user = await users.FindByIdForTokenIssueAsync(consumed.UserId, cancellationToken);
        if (user is null || !user.CanAuthenticate(now))
            return Result.Unauthorized();

        var refreshExpiresAt = now.AddDays(jwt.Value.RefreshTokenDays);
        var raw = refreshHasher.Generate();
        var child = RefreshToken.CreateChild(childId, consumed, raw.TokenHash, now, refreshExpiresAt);
        await refreshTokens.AddAsync(child, cancellationToken);
        await refreshTokens.SaveChangesAsync(cancellationToken);

        var access = tokenIssuer.Issue(user, now);
        var tokens = new AuthTokens(access.Value, access.ExpiresAt, raw.RawToken, refreshExpiresAt, user.MustChangePassword);

        // Cache so a retry of the just-consumed token within the grace window replays this exact pair.
        if (graceWindow > TimeSpan.Zero)
            replayCache.Set(presentedHash, tokens, graceWindow);

        return Result.Success(tokens);
    }
}
