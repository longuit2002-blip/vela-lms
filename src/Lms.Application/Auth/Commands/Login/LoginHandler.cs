using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Auth.Dtos;
using Lms.Domain.Identity;
using Mediator;
using Microsoft.Extensions.Options;

namespace Lms.Application.Auth.Commands.Login;

public sealed class LoginHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IRefreshTokenHasher refreshHasher,
    IJwtTokenIssuer tokenIssuer,
    IIdGenerator idGenerator,
    IOptions<LockoutOptions> lockout,
    IOptions<JwtOptions> jwt)
    : IRequestHandler<LoginCommand, Result<AuthTokens>>
{
    /// <summary>Error code the API maps to an account-locked problem (distinct from generic bad-credentials).</summary>
    public const string AccountLockedError = "account_locked";

    public async ValueTask<Result<AuthTokens>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var user = await users.FindByEmailForLoginAsync(command.Email, cancellationToken);
        if (user is null)
            return Result.Unauthorized(); // enumeration-safe: same response as a bad password

        if (!user.CanAuthenticate(now))
            return Result.Unauthorized(AccountLockedError);

        var verification = passwordHasher.Verify(user.PasswordHash, command.Password);
        if (!verification.Succeeded)
        {
            user.RecordFailedLogin(lockout.Value.MaxFailedAttempts, TimeSpan.FromMinutes(lockout.Value.LockoutMinutes), now);
            await users.SaveChangesAsync(cancellationToken);
            return Result.Unauthorized();
        }

        if (verification.NeedsRehash)
            user.UpgradePasswordHash(passwordHasher.Hash(command.Password), now);
        user.RecordSuccessfulLogin(now);

        // Mint the refresh-token family root + an access token.
        var refreshExpiresAt = now.AddDays(jwt.Value.RefreshTokenDays);
        var raw = refreshHasher.Generate();
        var root = RefreshToken.CreateRoot(
            idGenerator.NewId(), user.Id, user.OrganizationId, raw.TokenHash, now, refreshExpiresAt);
        await refreshTokens.AddAsync(root, cancellationToken);

        var access = tokenIssuer.Issue(user, now);

        // Single shared DbContext — one save persists the user changes and the new token.
        await users.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthTokens(access.Value, access.ExpiresAt, raw.RawToken, refreshExpiresAt, user.MustChangePassword));
    }
}
