namespace Lms.Application.Authorization;

/// <summary>
/// Marks a command/query as requiring a permission. The <see cref="AuthorizationBehavior{TMessage,TResponse}"/>
/// checks it before the handler runs and rejects the request (403) when the caller lacks the permission.
/// </summary>
public interface IRequirePermission
{
    /// <summary>The permission code the caller must hold (e.g. <c>departments.manage</c>).</summary>
    string Permission { get; }
}
