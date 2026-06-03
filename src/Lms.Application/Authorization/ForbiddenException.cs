namespace Lms.Application.Authorization;

/// <summary>
/// Thrown when an authenticated caller is denied an action — by RBAC (the
/// <see cref="AuthorizationBehavior{TMessage,TResponse}"/>) or by an ABAC guard inside a handler.
/// Both paths throw this one type so the API maps every denial to a single, uniform 403
/// <c>application/problem+json</c> shape (mirrors how <c>ValidationException</c> maps to 422).
/// </summary>
public sealed class ForbiddenException(string? reason = null)
    : Exception(reason ?? "You do not have permission to perform this action.");
