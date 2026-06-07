using Lms.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

/// <summary>
/// Read-only closure lookup backing the dept-branch ABAC guard. Runs under the tenant query filter
/// (request context), so it only matches closure rows in the caller's organization — a cross-tenant
/// ancestor/descendant pair simply returns false.
/// </summary>
public sealed class DepartmentClosure(AppDbContext db) : IDepartmentClosure
{
    public Task<bool> IsInBranchAsync(Guid ancestorDepartmentId, Guid descendantDepartmentId, CancellationToken cancellationToken)
        => db.DepartmentClosure.AnyAsync(
            c => c.AncestorId == ancestorDepartmentId && c.DescendantId == descendantDepartmentId,
            cancellationToken);
}
