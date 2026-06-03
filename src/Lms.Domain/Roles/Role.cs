using Lms.Domain.SeedWork;

namespace Lms.Domain.Roles;

/// <summary>
/// Role aggregate root — a named set of permission codes. A role is either a <em>system</em> role
/// (org-wide catalog, <see cref="OrganizationId"/> null, seeded once) or, in a later slice, an
/// org-defined custom role. This slice only creates system roles; users reference roles by
/// <c>code</c> (see <c>User.RoleCodes</c>), and effective permissions resolve from this catalog.
/// </summary>
public sealed class Role : Entity, IAggregateRoot
{
    private readonly List<string> _permissions = [];

    public Guid? OrganizationId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsSystem { get; private set; }

    public IReadOnlyCollection<string> Permissions => _permissions.AsReadOnly();

    // Required by EF Core for materialization.
    private Role() { }

    private Role(Guid id, Guid? organizationId, string code, string name, bool isSystem, IEnumerable<string> permissions)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        IsSystem = isSystem;
        _permissions.AddRange(permissions);
    }

    /// <summary>
    /// Creates a system role (org-wide, <see cref="OrganizationId"/> null). <paramref name="id"/>
    /// is supplied by the Application layer (UUID v7). Permission codes are trimmed, de-duplicated,
    /// and empties dropped.
    /// </summary>
    public static Role CreateSystem(Guid id, string code, string name, IReadOnlyCollection<string> permissions)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        ArgumentNullException.ThrowIfNull(permissions);

        return new Role(id, organizationId: null, NormalizeCode(code), NormalizeName(name), isSystem: true, NormalizePermissions(permissions));
    }

    private static string NormalizeCode(string? code)
    {
        code = (code ?? string.Empty).Trim();
        if (code.Length == 0)
            throw new ArgumentException("Code is required.", nameof(code));
        return code;
    }

    private static string NormalizeName(string? name)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ArgumentException("Name is required.", nameof(name));
        return name;
    }

    private static List<string> NormalizePermissions(IEnumerable<string> permissions) =>
        permissions
            .Select(p => (p ?? string.Empty).Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
}
