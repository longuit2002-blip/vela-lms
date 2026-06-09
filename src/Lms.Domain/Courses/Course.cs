using Lms.Domain.SeedWork;

namespace Lms.Domain.Courses;

/// <summary>
/// Course aggregate root — authored learning content owned by exactly one organization (the tenant).
/// It is the consistency boundary for its <see cref="Module"/>s and their <see cref="Lesson"/>s:
/// modules and lessons carry no organization id of their own and are only ever created through this
/// root, so they inherit tenancy transitively. The "at least one lesson to publish" rule is checked
/// by the publish handler (cross-aggregate) using <see cref="HasAnyLesson"/>; slug uniqueness within
/// the organization is enforced by the database, not here.
/// </summary>
public sealed class Course : Entity, IAggregateRoot
{
    private readonly List<Module> _modules = new();

    public Guid OrganizationId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public Guid? CategoryId { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? Description { get; private set; }
    public CourseStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<Module> Modules => _modules.AsReadOnly();
    public int LessonCount => _modules.Sum(m => m.Lessons.Count);
    public bool HasAnyLesson => LessonCount > 0;

    // Required by EF Core for materialization.
    private Course() { }

    private Course(Guid id, Guid organizationId, string title, string slug, DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        Title = title;
        Slug = slug;
        Status = CourseStatus.Draft;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a Draft course. <paramref name="id"/> is supplied by the Application layer (UUID v7).
    /// </summary>
    public static Course Create(Guid id, Guid organizationId, string title, string slug)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));

        return new Course(id, organizationId, NormalizeTitle(title), NormalizeSlug(slug), DateTimeOffset.UtcNow);
    }

    /// <summary>Appends an ordered module and returns it. <paramref name="id"/> is supplied by the caller.</summary>
    public Module AddModule(Guid id, string title, DateTimeOffset now)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        var module = new Module(id, Id, NormalizeTitle(title), _modules.Count + 1);
        _modules.Add(module);
        UpdatedAt = now;
        return module;
    }

    /// <summary>
    /// Appends an ordered video lesson under <paramref name="moduleId"/> and returns it.
    /// Throws if the module is not part of this course, the video URL is not a valid <c>https</c>
    /// URL, or the duration is not positive.
    /// </summary>
    public Lesson AddLesson(Guid moduleId, Guid lessonId, string title, string videoUrl, int durationSeconds, DateTimeOffset now)
    {
        if (lessonId == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(lessonId));

        var module = _modules.FirstOrDefault(m => m.Id == moduleId)
            ?? throw new ArgumentException("Module is not part of this course.", nameof(moduleId));

        if (durationSeconds <= 0)
            throw new ArgumentException("Duration must be positive.", nameof(durationSeconds));

        var lesson = module.AddLesson(lessonId, NormalizeTitle(title), NormalizeVideoUrl(videoUrl), durationSeconds);
        UpdatedAt = now;
        return lesson;
    }

    private static string NormalizeTitle(string? title)
    {
        title = (title ?? string.Empty).Trim();
        if (title.Length == 0)
            throw new ArgumentException("Title is required.", nameof(title));
        return title;
    }

    private static string NormalizeSlug(string? slug)
    {
        slug = (slug ?? string.Empty).Trim().ToLowerInvariant();
        if (slug.Length == 0)
            throw new ArgumentException("Slug is required.", nameof(slug));
        return slug;
    }

    private static string NormalizeVideoUrl(string? videoUrl)
    {
        videoUrl = (videoUrl ?? string.Empty).Trim();
        if (videoUrl.Length == 0)
            throw new ArgumentException("Video URL is required.", nameof(videoUrl));
        if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Video URL must be an absolute https URL.", nameof(videoUrl));
        return videoUrl;
    }
}
