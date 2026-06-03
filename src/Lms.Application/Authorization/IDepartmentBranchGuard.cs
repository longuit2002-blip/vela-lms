namespace Lms.Application.Authorization;

/// <summary>
/// Dept-branch ABAC guard. Called inside department handlers <b>after</b> RBAC has passed, to confine
/// branch-limited managers (those without <c>departments.manage.all</c>) to their own subtree. Throws
/// <see cref="ForbiddenException"/> (→ 403) when the target is out of branch. For reparent, the handler
/// calls it for <b>both</b> the moved department and the new parent.
/// </summary>
public interface IDepartmentBranchGuard
{
    Task EnsureCanManageAsync(Guid targetDepartmentId, CancellationToken cancellationToken);
}
