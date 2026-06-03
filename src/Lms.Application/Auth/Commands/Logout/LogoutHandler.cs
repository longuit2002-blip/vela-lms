using Ardalis.Result;
using Lms.Application.Abstractions;
using Mediator;

namespace Lms.Application.Auth.Commands.Logout;

public sealed class LogoutHandler(IRefreshTokenRepository refreshTokens, IRefreshTokenHasher refreshHasher)
    : IRequestHandler<LogoutCommand, Result>
{
    public async ValueTask<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            var hash = refreshHasher.Hash(command.RefreshToken);
            var token = await refreshTokens.FindByHashAsync(hash, cancellationToken);
            if (token is not null)
                await refreshTokens.RevokeFamilyAsync(token.FamilyId, "logout", DateTimeOffset.UtcNow, cancellationToken);
        }

        return Result.Success();
    }
}
