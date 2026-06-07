using Lms.Application.Abstractions;
using Lms.Domain.Departments;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

/// <summary>
/// Department persistence + closure-table maintenance. Lookups (<see cref="FindByIdAsync"/>,
/// <see cref="ListByOrganizationAsync"/>) honor the tenant query filter (never bypassed — that is the
/// IDOR control). Closure reads/writes scope by the department's own <c>OrganizationId</c> with
/// <c>IgnoreQueryFilters</c>, so they are correct both in a request (ambient org == department org)
/// and under the no-tenant-context seed path (ambient org is <see cref="Guid.Empty"/>).
/// </summary>
public sealed class DepartmentRepository(AppDbContext db) : IDepartmentRepository
{
    public async Task AddAsync(Department department, CancellationToken cancellationToken)
    {
        await db.Departments.AddAsync(department, cancellationToken);

        var orgId = department.OrganizationId;
        var rows = new List<DepartmentClosureRow>
        {
            new(orgId, department.Id, department.Id, 0), // self link, depth 0
        };

        if (department.ParentId is { } parentId)
        {
            // Inherit every ancestor of the parent (the parent itself is at depth 0 in its own closure).
            // Requires the parent's closure rows to be persisted already (the seed saves per node).
            var parentAncestors = await db.DepartmentClosure
                .IgnoreQueryFilters()
                .Where(c => c.OrganizationId == orgId && c.DescendantId == parentId)
                .ToListAsync(cancellationToken);

            rows.AddRange(parentAncestors.Select(a =>
                new DepartmentClosureRow(orgId, a.AncestorId, department.Id, a.Depth + 1)));
        }

        await db.DepartmentClosure.AddRangeAsync(rows, cancellationToken);
    }

    public Task<Department?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Department>> ListByOrganizationAsync(CancellationToken cancellationToken)
        => await db.Departments.OrderBy(d => d.Name).ToListAsync(cancellationToken);

    /// <summary>
    /// Persists the reparent atomically: the aggregate's already-updated <c>ParentId</c>/<c>UpdatedAt</c>
    /// plus the closure rebuild, in one transaction. Uses the canonical closure-move SQL (delete the
    /// moved subtree's links to its old ancestors, then insert links from the new ancestors with
    /// recomputed depth) — raw SQL avoids the EF identity-map collision when a shared ancestor's
    /// depth row is both removed and re-added. The Move handler calls this <b>instead of</b> a
    /// separate SaveChanges. Caller must have run the cycle check first.
    /// </summary>
    public async Task ReparentAsync(Department department, CancellationToken cancellationToken)
    {
        var orgId = department.OrganizationId;
        var deptId = department.Id;
        var newParentId = department.ParentId;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        // Persist the aggregate's parent_id + updated_at change.
        await db.SaveChangesAsync(cancellationToken);

        // Remove links from the moved subtree to ancestors outside the subtree (stale, incl. shared
        // ancestors whose depth changes — they are re-inserted below at the correct depth).
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM department_closure
            WHERE organization_id = {orgId}
              AND descendant_id IN (
                  SELECT descendant_id FROM department_closure
                  WHERE organization_id = {orgId} AND ancestor_id = {deptId})
              AND ancestor_id NOT IN (
                  SELECT descendant_id FROM department_closure
                  WHERE organization_id = {orgId} AND ancestor_id = {deptId})", cancellationToken);

        if (newParentId is { } np)
        {
            // New external links = (new parent + its ancestors) × (moved subtree nodes), depth summed.
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO department_closure (organization_id, ancestor_id, descendant_id, depth)
                SELECT {orgId}, super.ancestor_id, sub.descendant_id, super.depth + sub.depth + 1
                FROM department_closure super
                CROSS JOIN department_closure sub
                WHERE super.organization_id = {orgId} AND super.descendant_id = {np}
                  AND sub.organization_id = {orgId} AND sub.ancestor_id = {deptId}", cancellationToken);
        }
        // newParentId null → department becomes a root; no external ancestor links needed.

        await tx.CommitAsync(cancellationToken);
    }

    public async Task RemoveAsync(Department department, CancellationToken cancellationToken)
    {
        // Caller has enforced block-if-non-empty (no children, no assigned users), so this is a leaf:
        // its only closure rows are the self link and links from its ancestors down to it.
        var orgId = department.OrganizationId;
        var closureRows = await db.DepartmentClosure
            .IgnoreQueryFilters()
            .Where(c => c.OrganizationId == orgId && (c.AncestorId == department.Id || c.DescendantId == department.Id))
            .ToListAsync(cancellationToken);

        db.DepartmentClosure.RemoveRange(closureRows);
        db.Departments.Remove(department);
    }

    public Task<bool> HasChildrenAsync(Guid departmentId, CancellationToken cancellationToken)
        => db.Departments.AnyAsync(d => d.ParentId == departmentId, cancellationToken);

    public Task<bool> HasAssignedUsersAsync(Guid departmentId, CancellationToken cancellationToken)
        => db.Users.AnyAsync(u => u.DepartmentId == departmentId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
