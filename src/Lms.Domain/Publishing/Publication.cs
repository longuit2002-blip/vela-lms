using Lms.Domain.SeedWork;

namespace Lms.Domain.Publishing;

/// <summary>
/// Publication aggregate root — the unified facade for publishing content. Enrollments point at a
/// Publication (never directly at a Course) so one progress mechanism can later serve courses,
/// paths, and exams. This first slice models only the minimal facade: a course publication that
/// flips Draft → Published.
/// </summary>
/// <remarks>
/// The canonical model also requires "at least one audience target" to publish; that invariant is
/// deferred with audience-scope targeting to a later slice. When it lands, <see cref="Publish"/>
/// gains an audience-target precondition and enrollment creation moves to publish-time fan-out
/// (replacing the temporary explicit-assign command).
/// </remarks>
public sealed class Publication : Entity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }

    /// <summary>Canonical content discriminator (e.g. <c>course</c>). Carried now so no back-fill is needed later.</summary>
    public string Kind { get; private set; } = null!;

    /// <summary>The published content's id — a Course id for <c>course</c> publications.</summary>
    public Guid ContentId { get; private set; }

    public string Title { get; private set; } = null!;
    public PublicationStatus Status { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid? PublishedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private const string CourseKind = "course";

    // Required by EF Core for materialization.
    private Publication() { }

    private Publication(Guid id, Guid organizationId, string kind, Guid contentId, string title, DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        Kind = kind;
        ContentId = contentId;
        Title = title;
        Status = PublicationStatus.Draft;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Creates a Draft publication for a course. <paramref name="id"/> is supplied by the Application layer.</summary>
    public static Publication CreateForCourse(Guid id, Guid organizationId, Guid courseId, string title)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (courseId == Guid.Empty)
            throw new ArgumentException("CourseId is required.", nameof(courseId));

        return new Publication(id, organizationId, CourseKind, courseId, NormalizeTitle(title), DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Publishes the publication. The "course has at least one lesson" precondition is a
    /// cross-aggregate rule enforced by the publish handler before calling this.
    /// </summary>
    public void Publish(Guid publishedBy, DateTimeOffset now)
    {
        if (Status != PublicationStatus.Draft)
            throw new InvalidOperationException($"Only a Draft publication can be published (was {Status}).");
        if (publishedBy == Guid.Empty)
            throw new ArgumentException("PublishedBy is required.", nameof(publishedBy));

        Status = PublicationStatus.Published;
        PublishedAt = now;
        PublishedBy = publishedBy;
        UpdatedAt = now;
    }

    private static string NormalizeTitle(string? title)
    {
        title = (title ?? string.Empty).Trim();
        if (title.Length == 0)
            throw new ArgumentException("Title is required.", nameof(title));
        return title;
    }
}
