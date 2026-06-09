using Lms.Domain.SeedWork;

namespace Lms.Domain.Courses;

/// <summary>
/// A chapter inside a <see cref="Course"/>, grouping ordered <see cref="Lesson"/>s. A module is an
/// entity within the Course aggregate — it is created and mutated only through the <see cref="Course"/>
/// root (<see cref="Course.AddModule"/> / <see cref="Course.AddLesson"/>), never directly.
/// </summary>
public sealed class Module : Entity
{
    private readonly List<Lesson> _lessons = new();

    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = null!;
    public int Order { get; private set; }
    public IReadOnlyList<Lesson> Lessons => _lessons.AsReadOnly();

    // Required by EF Core for materialization.
    private Module() { }

    internal Module(Guid id, Guid courseId, string title, int order)
    {
        Id = id;
        CourseId = courseId;
        Title = title;
        Order = order;
    }

    internal Lesson AddLesson(Guid id, string title, string videoUrl, int durationSeconds)
    {
        var lesson = new Lesson(id, Id, title, _lessons.Count + 1, videoUrl, durationSeconds);
        _lessons.Add(lesson);
        return lesson;
    }
}
