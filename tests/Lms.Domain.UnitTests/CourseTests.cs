using Lms.Domain.Courses;

namespace Lms.Domain.UnitTests;

public class CourseTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private static Course NewCourse() =>
        Course.Create(Guid.NewGuid(), OrgId, "  Customer Service 101  ", "  CS-101  ");

    [Fact]
    public void Create_sets_fields_normalized_and_draft()
    {
        var id = Guid.NewGuid();
        var course = Course.Create(id, OrgId, "  Customer Service 101  ", "  CS-101  ");

        Assert.Equal(id, course.Id);
        Assert.Equal(OrgId, course.OrganizationId);
        Assert.Equal("Customer Service 101", course.Title);
        Assert.Equal("cs-101", course.Slug); // trimmed + lowercased
        Assert.Equal(CourseStatus.Draft, course.Status);
        Assert.Equal(course.CreatedAt, course.UpdatedAt);
        Assert.False(course.HasAnyLesson);
        Assert.Empty(course.Modules);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_title(string title) =>
        Assert.Throws<ArgumentException>(() => Course.Create(Guid.NewGuid(), OrgId, title, "slug"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_slug(string slug) =>
        Assert.Throws<ArgumentException>(() => Course.Create(Guid.NewGuid(), OrgId, "Title", slug));

    [Fact]
    public void Create_rejects_empty_id_and_org()
    {
        Assert.Throws<ArgumentException>(() => Course.Create(Guid.Empty, OrgId, "Title", "slug"));
        Assert.Throws<ArgumentException>(() => Course.Create(Guid.NewGuid(), Guid.Empty, "Title", "slug"));
    }

    [Fact]
    public void AddModule_appends_with_sequential_order_and_bumps_timestamp()
    {
        var course = NewCourse();
        var later = course.UpdatedAt.AddMinutes(1);

        var m1 = course.AddModule(Guid.NewGuid(), "  Intro  ", later);
        var m2 = course.AddModule(Guid.NewGuid(), "Advanced", later);

        Assert.Equal("Intro", m1.Title);
        Assert.Equal(1, m1.Order);
        Assert.Equal(2, m2.Order);
        Assert.Equal(2, course.Modules.Count);
        Assert.Equal(later, course.UpdatedAt);
    }

    [Fact]
    public void AddLesson_appends_under_module_with_order_and_stores_video_fields()
    {
        var course = NewCourse();
        var module = course.AddModule(Guid.NewGuid(), "Intro", course.CreatedAt);

        var l1 = course.AddLesson(module.Id, Guid.NewGuid(), "  Welcome  ", "https://cdn.example.com/a.mp4", 120, course.CreatedAt);
        var l2 = course.AddLesson(module.Id, Guid.NewGuid(), "Next", "https://cdn.example.com/b.mp4", 90, course.CreatedAt);

        Assert.Equal("Welcome", l1.Title);
        Assert.Equal(LessonType.Video, l1.Type);
        Assert.Equal("https://cdn.example.com/a.mp4", l1.VideoUrl);
        Assert.Equal(120, l1.DurationSeconds);
        Assert.Equal(1, l1.Order);
        Assert.Equal(2, l2.Order);
        Assert.Equal(2, course.LessonCount);
        Assert.True(course.HasAnyLesson);
    }

    [Fact]
    public void AddLesson_to_unknown_module_throws()
    {
        var course = NewCourse();
        Assert.Throws<ArgumentException>(() =>
            course.AddLesson(Guid.NewGuid(), Guid.NewGuid(), "L", "https://cdn.example.com/a.mp4", 60, course.CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://insecure.example.com/a.mp4")]
    [InlineData("file:///etc/passwd")]
    [InlineData("not-a-url")]
    public void AddLesson_rejects_blank_or_non_https_video_url(string url)
    {
        var course = NewCourse();
        var module = course.AddModule(Guid.NewGuid(), "Intro", course.CreatedAt);
        Assert.Throws<ArgumentException>(() =>
            course.AddLesson(module.Id, Guid.NewGuid(), "L", url, 60, course.CreatedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddLesson_rejects_non_positive_duration(int seconds)
    {
        var course = NewCourse();
        var module = course.AddModule(Guid.NewGuid(), "Intro", course.CreatedAt);
        Assert.Throws<ArgumentException>(() =>
            course.AddLesson(module.Id, Guid.NewGuid(), "L", "https://cdn.example.com/a.mp4", seconds, course.CreatedAt));
    }

    [Fact]
    public void LessonCount_aggregates_across_modules()
    {
        var course = NewCourse();
        var m1 = course.AddModule(Guid.NewGuid(), "M1", course.CreatedAt);
        var m2 = course.AddModule(Guid.NewGuid(), "M2", course.CreatedAt);
        course.AddLesson(m1.Id, Guid.NewGuid(), "L1", "https://cdn.example.com/a.mp4", 60, course.CreatedAt);
        course.AddLesson(m2.Id, Guid.NewGuid(), "L2", "https://cdn.example.com/b.mp4", 60, course.CreatedAt);
        course.AddLesson(m2.Id, Guid.NewGuid(), "L3", "https://cdn.example.com/c.mp4", 60, course.CreatedAt);

        Assert.Equal(3, course.LessonCount);
    }
}
