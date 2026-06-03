namespace Lms.Infrastructure.Persistence;

/// <summary>
/// A row in the department closure table — one entry per (ancestor, descendant) pair, including the
/// depth-0 self link. This is a persistence-derived index that makes subtree membership and
/// branch-scoped (ABAC) checks a single lookup; it is not a domain aggregate. Carries
/// <see cref="OrganizationId"/> (denormalized from the department) so it joins the tenant
/// EF filter + RLS regime uniformly with the other tenant tables.
/// </summary>
public sealed class DepartmentClosureRow
{
    public Guid OrganizationId { get; private set; }
    public Guid AncestorId { get; private set; }
    public Guid DescendantId { get; private set; }
    public int Depth { get; private set; }

    // Required by EF Core for materialization.
    private DepartmentClosureRow() { }

    public DepartmentClosureRow(Guid organizationId, Guid ancestorId, Guid descendantId, int depth)
    {
        OrganizationId = organizationId;
        AncestorId = ancestorId;
        DescendantId = descendantId;
        Depth = depth;
    }
}
