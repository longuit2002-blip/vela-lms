using Ardalis.Result;
using Lms.Application.Auth.Dtos;
using Mediator;

namespace Lms.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Changes the authenticated user's password, clears the forced-change flag, revokes existing
/// refresh tokens, and returns a fresh token pair (so the new access token no longer carries the
/// must-change-password gate claim).
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result<AuthTokens>>;
