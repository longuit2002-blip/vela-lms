using Lms.Domain.SeedWork;

namespace Lms.Domain.Departments;

/// <summary>
/// Department aggregate root — a node in an organization's tree. Belongs to exactly one
/// organization (the tenant) and optionally to a parent department. The acyclic-tree invariant
/// across the whole subtree is enforced where the closure table is available (the Application
/// handler / repository, see the 003 plan U6); the aggregate only guards the local self-parent case.
/// </summary>
public sealed class Department : Entity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Required by EF Core for materialization.
    private Department() { }

    private Department(Guid id, Guid organizationId, Guid? parentId, string name, DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        ParentId = parentId;
        Name = name;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a department, optionally under <paramref name="parentId"/> (null = a root node).
    /// <paramref name="id"/> is supplied by the Application layer (UUID v7).
    /// </summary>
    public static Department Create(Guid id, Guid organizationId, Guid? parentId, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (parentId == id)
            throw new ArgumentException("A department cannot be its own parent.", nameof(parentId));

        return new Department(id, organizationId, parentId, NormalizeName(name), DateTimeOffset.UtcNow);
    }

    /// <summary>Renames the department.</summary>
    public void Rename(string name, DateTimeOffset now)
    {
        Name = NormalizeName(name);
        UpdatedAt = now;
    }

    /// <summary>
    /// Moves the department under a new parent (null = make it a root). Rejects the local
    /// self-parent case; the subtree-cycle check (<c>newParentId ∈ subtree(this)</c>) is the
    /// caller's responsibility via the closure table, since the aggregate cannot see descendants.
    /// </summary>
    public void Reparent(Guid? newParentId, DateTimeOffset now)
    {
        if (newParentId == Id)
            throw new ArgumentException("A department cannot be its own parent.", nameof(newParentId));

        ParentId = newParentId;
        UpdatedAt = now;
    }

    private static string NormalizeName(string? name)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ArgumentException("Name is required.", nameof(name));
        return name;
    }
}
