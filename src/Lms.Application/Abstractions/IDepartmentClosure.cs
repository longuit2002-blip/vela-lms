namespace Lms.Application.Abstractions;

/// <summary>
/// Narrow read-only port for the dept-branch ABAC guard: answers "is <paramref name="descendantDepartmentId"/>
/// within the subtree rooted at <paramref name="ancestorDepartmentId"/>?" via the closure table.
/// Subtree includes the ancestor itself (depth-0 self link), so a manager may act on their own
/// department node and everything beneath it. Kept separate from <see cref="IDepartmentRepository"/>
/// so the guard depends only on this check, not on CRUD.
/// </summary>
public interface IDepartmentClosure
{
    Task<bool> IsInBranchAsync(Guid ancestorDepartmentId, Guid descendantDepartmentId, CancellationToken cancellationToken);
}
