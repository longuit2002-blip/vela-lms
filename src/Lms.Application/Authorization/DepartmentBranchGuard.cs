using Lms.Application.Abstractions;

namespace Lms.Application.Authorization;

/// <inheritdoc />
public sealed class DepartmentBranchGuard(
    ICurrentUser currentUser,
    IPermissionResolver permissionResolver,
    IDepartmentClosure closure) : IDepartmentBranchGuard
{
    public async Task EnsureCanManageAsync(Guid targetDepartmentId, CancellationToken cancellationToken)
    {
        var granted = await permissionResolver.ResolveAsync(currentUser.UserId, currentUser.RoleCodes, cancellationToken);

        // Full-scope (OrgOwner/OrgAdmin hold departments.manage.all) → no branch restriction.
        if (granted.Contains(Permissions.Departments.ManageAll))
            return;

        // Branch-limited: fail closed if the manager has no department scope, then require the target
        // to be within the manager's subtree (includes the manager's own department node).
        if (currentUser.CurrentDepartmentId is not { } callerDepartmentId)
            throw new ForbiddenException("A branch-limited manager has no department scope.");

        if (!await closure.IsInBranchAsync(callerDepartmentId, targetDepartmentId, cancellationToken))
            throw new ForbiddenException("The target department is outside your branch.");
    }

    public async Task EnsureFullScopeAsync(CancellationToken cancellationToken)
    {
        var granted = await permissionResolver.ResolveAsync(currentUser.UserId, currentUser.RoleCodes, cancellationToken);
        if (!granted.Contains(Permissions.Departments.ManageAll))
            throw new ForbiddenException("This action requires organization-wide (full-scope) management.");
    }
}
