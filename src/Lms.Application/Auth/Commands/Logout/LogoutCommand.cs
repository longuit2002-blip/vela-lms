using Ardalis.Result;
using Mediator;

namespace Lms.Application.Auth.Commands.Logout;

/// <summary>Revokes the presented refresh token's family. Idempotent — succeeds even if unknown.</summary>
public sealed record LogoutCommand(string? RefreshToken) : IRequest<Result>;
