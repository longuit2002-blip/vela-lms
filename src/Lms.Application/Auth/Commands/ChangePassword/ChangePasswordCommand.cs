using Ardalis.Result;
using Lms.Application.Auth.Dtos;
using Mediator;

namespace Lms.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Changes the authenticated user's password, clears the forced-change flag, revokes existing
/// refresh tokens, and returns a fresh token pair (so the new access token no longer carries the
/// must-change-password gate claim).
/// <para>
/// Authorization audit (U8): intentionally <b>not</b> gated by an <c>IRequirePermission</c> — this is a
/// self-scoped action (a user changing their own password) protected by authentication plus the
/// current-password check, and it must also work while the forced-change gate is active.
/// </para>
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result<AuthTokens>>;
