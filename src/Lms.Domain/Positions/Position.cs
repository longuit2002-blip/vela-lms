using Lms.Domain.SeedWork;

namespace Lms.Domain.Positions;

/// <summary>
/// Position aggregate root — a job title in an organization. A flat, org-wide catalog (no
/// hierarchy): positions classify users but do not drive branch-scoped (ABAC) authorization.
/// Name uniqueness per organization is enforced by a database constraint, not the aggregate.
/// </summary>
public sealed class Position : Entity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Required by EF Core for materialization.
    private Position() { }

    private Position(Guid id, Guid organizationId, string name, DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a position. <paramref name="id"/> is supplied by the Application layer (UUID v7).
    /// </summary>
    public static Position Create(Guid id, Guid organizationId, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));

        return new Position(id, organizationId, NormalizeName(name), DateTimeOffset.UtcNow);
    }

    /// <summary>Renames the position.</summary>
    public void Rename(string name, DateTimeOffset now)
    {
        Name = NormalizeName(name);
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
