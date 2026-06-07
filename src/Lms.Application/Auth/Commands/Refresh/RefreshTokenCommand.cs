using Ardalis.Result;
using Lms.Application.Auth.Dtos;
using Mediator;

namespace Lms.Application.Auth.Commands.Refresh;

/// <summary>Exchanges a refresh token for a new access + refresh pair (rotation with reuse detection).</summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokens>>;
