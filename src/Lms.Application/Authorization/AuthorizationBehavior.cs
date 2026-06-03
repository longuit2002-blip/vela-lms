using Lms.Application.Abstractions;
using Mediator;

namespace Lms.Application.Authorization;

/// <summary>
/// RBAC enforcement. For any message implementing <see cref="IRequirePermission"/>, checks the
/// caller's effective permissions before the handler runs and throws <see cref="ForbiddenException"/>
/// (→ 403) when the permission is absent. Registered <b>before</b> <c>ValidationBehavior</c> via the
/// compile-time <c>PipelineBehaviors</c> array (see <c>DependencyInjection</c>), so an unauthorized
/// caller is denied before validation runs — input-shape (422) detail never leaks to callers who
/// lack permission. Mirrors <c>ValidationBehavior</c>: throws rather than returning a typed Result.
/// </summary>
public sealed class AuthorizationBehavior<TMessage, TResponse>(
    ICurrentUser currentUser,
    IPermissionResolver permissionResolver)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (message is IRequirePermission requirement)
        {
            // Fail closed: an unauthenticated caller (endpoint missing .RequireAuthorization, or a
            // non-HTTP context) is denied before any permission resolution.
            if (!currentUser.IsAuthenticated)
                throw new ForbiddenException();

            var granted = await permissionResolver.ResolveAsync(
                currentUser.UserId, currentUser.RoleCodes, cancellationToken);

            if (!granted.Contains(requirement.Permission))
                throw new ForbiddenException($"Missing required permission '{requirement.Permission}'.");
        }

        return await next(message, cancellationToken);
    }
}
