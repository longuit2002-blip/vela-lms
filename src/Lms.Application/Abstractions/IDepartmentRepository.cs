using Lms.Domain.Departments;

namespace Lms.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="Department"/> aggregate and its closure table. Focused
/// (no generic repository), implemented in Infrastructure over EF Core. Closure maintenance is
/// scoped by the department's own <c>OrganizationId</c> (not the ambient tenant filter) so it is
/// correct under the no-tenant-context seed path as well as normal requests.
/// </summary>
public interface IDepartmentRepository
{
    /// <summary>Stages the department plus its closure rows (self link + the parent's inherited ancestors).</summary>
    Task AddAsync(Department department, CancellationToken cancellationToken);

    /// <summary>Tenant-scoped lookup by id (honors the query filter — never bypassed, for IDOR safety).</summary>
    Task<Department?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>All departments in the current tenant (tree nodes; caller derives parent/child from <c>ParentId</c>).</summary>
    Task<IReadOnlyList<Department>> ListByOrganizationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Rebuilds the closure for the moved subtree after <paramref name="department"/>'s <c>ParentId</c>
    /// has been updated to its new parent: removes links from the subtree to its old ancestors and
    /// inserts links from the new ancestors. Caller must have already run the cycle check.
    /// </summary>
    Task ReparentAsync(Department department, CancellationToken cancellationToken);

    /// <summary>Removes the department (its closure rows cascade). Caller must enforce the delete-block first.</summary>
    Task RemoveAsync(Department department, CancellationToken cancellationToken);

    /// <summary>True if the department has at least one child department.</summary>
    Task<bool> HasChildrenAsync(Guid departmentId, CancellationToken cancellationToken);

    /// <summary>True if at least one user is assigned to the department.</summary>
    Task<bool> HasAssignedUsersAsync(Guid departmentId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
