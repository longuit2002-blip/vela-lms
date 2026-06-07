using System.Text.RegularExpressions;
using Lms.Domain.SeedWork;

namespace Lms.Domain.Organizations;

/// <summary>
/// Organization aggregate root — a tenant in the LMS. Minimal for the walking skeleton:
/// later phases add departments, positions, settings, and audience scopes.
/// </summary>
public sealed partial class Organization : Entity, IAggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public OrganizationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Required by EF Core for materialization.
    private Organization() { }

    private Organization(Guid id, string name, string slug, DateTimeOffset now)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Status = OrganizationStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a new active organization. <paramref name="id"/> is supplied by the
    /// Application layer (UUID v7 generator) so the Domain stays free of infrastructure concerns.
    /// </summary>
    public static Organization Create(Guid id, string name, string slug)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        name = (name ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ArgumentException("Name is required.", nameof(name));

        slug = NormalizeSlug(slug);

        return new Organization(id, name, slug, DateTimeOffset.UtcNow);
    }

    private static string NormalizeSlug(string? slug)
    {
        slug = (slug ?? string.Empty).Trim().ToLowerInvariant();
        if (slug.Length == 0)
            throw new ArgumentException("Slug is required.", nameof(slug));
        if (!SlugPattern().IsMatch(slug))
            throw new ArgumentException("Slug must be lowercase alphanumeric segments separated by single hyphens.", nameof(slug));
        return slug;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
