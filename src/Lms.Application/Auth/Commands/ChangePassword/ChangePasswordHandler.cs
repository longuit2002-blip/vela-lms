using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Auth.Dtos;
using Lms.Domain.Identity;
using Mediator;
using Microsoft.Extensions.Options;

namespace Lms.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordHandler(
    ICurrentUser currentUser,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IRefreshTokenHasher refreshHasher,
    IJwtTokenIssuer tokenIssuer,
    IIdGenerator idGenerator,
    IOptions<JwtOptions> jwt)
    : IRequestHandler<ChangePasswordCommand, Result<AuthTokens>>
{
    public const string CurrentPasswordIncorrectError = "current_password_incorrect";

    public async ValueTask<Result<AuthTokens>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            return Result.Unauthorized();

        var user = await users.FindByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
            return Result.Unauthorized();

        var verification = passwordHasher.Verify(user.PasswordHash, command.CurrentPassword);
        if (!verification.Succeeded)
            return Result.Unauthorized(CurrentPasswordIncorrectError);

        var now = DateTimeOffset.UtcNow;
        user.ChangePassword(passwordHasher.Hash(command.NewPassword), now);

        // Force re-login everywhere, then mint a fresh family + access token (clears the mcp claim).
        await refreshTokens.RevokeAllForUserAsync(user.Id, "password_changed", now, cancellationToken);

        var refreshExpiresAt = now.AddDays(jwt.Value.RefreshTokenDays);
        var raw = refreshHasher.Generate();
        var root = RefreshToken.CreateRoot(
            idGenerator.NewId(), user.Id, user.OrganizationId, raw.TokenHash, now, refreshExpiresAt);
        await refreshTokens.AddAsync(root, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        var access = tokenIssuer.Issue(user, now);
        return Result.Success(new AuthTokens(access.Value, access.ExpiresAt, raw.RawToken, refreshExpiresAt, user.MustChangePassword));
    }
}
