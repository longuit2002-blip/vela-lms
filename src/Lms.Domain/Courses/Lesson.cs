using Lms.Domain.SeedWork;

namespace Lms.Domain.Courses;

/// <summary>
/// A single learnable unit inside a <see cref="Module"/>. In the first slice every lesson is a
/// <see cref="LessonType.Video"/> carrying a plain <c>https</c> URL (no MediaAsset/HLS/signed URL
/// yet — see the media-pipeline slice). A lesson is an entity within the <see cref="Course"/>
/// aggregate; it is only ever created through <see cref="Course.AddLesson"/>.
/// </summary>
public sealed class Lesson : Entity
{
    public Guid ModuleId { get; private set; }
    public string Title { get; private set; } = null!;
    public int Order { get; private set; }
    public LessonType Type { get; private set; }
    public string VideoUrl { get; private set; } = null!;
    public int DurationSeconds { get; private set; }

    // Required by EF Core for materialization.
    private Lesson() { }

    internal Lesson(Guid id, Guid moduleId, string title, int order, string videoUrl, int durationSeconds)
    {
        Id = id;
        ModuleId = moduleId;
        Title = title;
        Order = order;
        Type = LessonType.Video;
        VideoUrl = videoUrl;
        DurationSeconds = durationSeconds;
    }
}
