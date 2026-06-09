using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Courses.Commands.AddLesson;
using Lms.Application.Courses.Commands.AddModule;
using Lms.Application.Courses.Commands.CreateCourse;
using Lms.Application.Courses.Queries.GetCourse;
using Lms.Domain.Courses;

namespace Lms.Application.UnitTests;

public class CourseAuthoringHandlersTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private sealed class FakeCourseRepository : ICourseRepository
    {
        public Dictionary<Guid, Course> Store { get; } = new();
        public int SaveCount { get; private set; }

        public Task AddAsync(Course course, CancellationToken ct) { Store[course.Id] = course; return Task.CompletedTask; }
        public void AddModule(Module module) { }
        public void AddLesson(Lesson lesson) { }
        public Task<Course?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Store.GetValueOrDefault(id));
        public Task<IReadOnlyList<Course>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Course>>([.. Store.Values.Where(c => ids.Contains(c.Id))]);
        public Task SaveChangesAsync(CancellationToken ct) { SaveCount++; return Task.CompletedTask; }
    }

    private sealed class FakeTenant(Guid orgId) : ITenantContext { public Guid OrganizationId => orgId; }

    private sealed class FakeIdGen : IIdGenerator { public Guid NewId() => Guid.NewGuid(); }

    private static CreateCourseHandler CreateHandler(FakeCourseRepository repo, Guid? org = null)
        => new(repo, new FakeIdGen(), new FakeTenant(org ?? OrgId));

    [Fact]
    public async Task CreateCourse_returns_created_and_persists()
    {
        var repo = new FakeCourseRepository();
        var result = await CreateHandler(repo).Handle(new CreateCourseCommand("Customer Service", "cs-101"), CancellationToken.None);

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal("Customer Service", result.Value.Title);
        Assert.Single(repo.Store);
        Assert.Equal(1, repo.SaveCount);
    }

    [Fact]
    public async Task CreateCourse_without_tenant_is_unauthorized()
    {
        var repo = new FakeCourseRepository();
        var result = await CreateHandler(repo, Guid.Empty).Handle(new CreateCourseCommand("T", "t"), CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.Empty(repo.Store);
    }

    [Fact]
    public async Task AddModule_to_missing_course_is_not_found()
    {
        var repo = new FakeCourseRepository();
        var handler = new AddModuleHandler(repo, new FakeIdGen(), new FakeTenant(OrgId));

        var result = await handler.Handle(new AddModuleCommand(Guid.NewGuid(), "Intro"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task AddLesson_appends_and_returns_created()
    {
        var repo = new FakeCourseRepository();
        var course = Course.Create(Guid.NewGuid(), OrgId, "C", "c");
        var module = course.AddModule(Guid.NewGuid(), "M", DateTimeOffset.UtcNow);
        repo.Store[course.Id] = course;

        var handler = new AddLessonHandler(repo, new FakeIdGen(), new FakeTenant(OrgId));
        var result = await handler.Handle(
            new AddLessonCommand(course.Id, module.Id, "Welcome", "https://cdn.example.com/a.mp4", 120), CancellationToken.None);

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal(1, result.Value.Order);
        Assert.Equal("https://cdn.example.com/a.mp4", result.Value.VideoUrl);
        Assert.Equal(1, course.LessonCount);
    }

    [Fact]
    public async Task AddLesson_to_unknown_module_is_not_found()
    {
        var repo = new FakeCourseRepository();
        var course = Course.Create(Guid.NewGuid(), OrgId, "C", "c");
        repo.Store[course.Id] = course;

        var handler = new AddLessonHandler(repo, new FakeIdGen(), new FakeTenant(OrgId));
        var result = await handler.Handle(
            new AddLessonCommand(course.Id, Guid.NewGuid(), "L", "https://cdn.example.com/a.mp4", 60), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetCourse_returns_detail_with_modules_and_lessons()
    {
        var repo = new FakeCourseRepository();
        var course = Course.Create(Guid.NewGuid(), OrgId, "C", "c");
        var module = course.AddModule(Guid.NewGuid(), "M", DateTimeOffset.UtcNow);
        course.AddLesson(module.Id, Guid.NewGuid(), "L", "https://cdn.example.com/a.mp4", 60, DateTimeOffset.UtcNow);
        repo.Store[course.Id] = course;

        var result = await new GetCourseHandler(repo, new FakeTenant(OrgId))
            .Handle(new GetCourseQuery(course.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Modules);
        Assert.Single(result.Value.Modules[0].Lessons);
    }

    [Fact]
    public async Task GetCourse_missing_is_not_found()
    {
        var result = await new GetCourseHandler(new FakeCourseRepository(), new FakeTenant(OrgId))
            .Handle(new GetCourseQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
