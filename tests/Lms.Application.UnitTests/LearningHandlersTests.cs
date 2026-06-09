using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Learning.Commands.CompleteLesson;
using Lms.Application.Learning.Queries.GetEnrolledCourseDetail;
using Lms.Application.Learning.Queries.GetMyEnrollments;
using Lms.Domain.Courses;
using Lms.Domain.Learning;
using Lms.Domain.Publishing;

namespace Lms.Application.UnitTests;

public class LearningHandlersTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid LearnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private sealed class FakeEnrollmentRepo : IEnrollmentRepository
    {
        public Dictionary<Guid, Enrollment> Store { get; } = new();
        public Task AddRangeAsync(IEnumerable<Enrollment> e, CancellationToken ct) { foreach (var x in e) Store[x.Id] = x; return Task.CompletedTask; }
        public Task<Enrollment?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Store.GetValueOrDefault(id));
        public Task<IReadOnlyList<Enrollment>> ListByUserAsync(Guid u, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Enrollment>>([.. Store.Values.Where(e => e.UserId == u)]);
        public Task<bool> ExistsAsync(Guid u, Guid p, CancellationToken ct) => Task.FromResult(Store.Values.Any(e => e.UserId == u && e.PublicationId == p));
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakePublicationRepo : IPublicationRepository
    {
        public Dictionary<Guid, Publication> Store { get; } = new();
        public Task AddAsync(Publication p, CancellationToken ct) { Store[p.Id] = p; return Task.CompletedTask; }
        public Task<Publication?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Store.GetValueOrDefault(id));
        public Task<IReadOnlyList<Publication>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Publication>>([.. Store.Values.Where(p => ids.Contains(p.Id))]);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeCourseRepo : ICourseRepository
    {
        public Dictionary<Guid, Course> Store { get; } = new();
        public Task AddAsync(Course c, CancellationToken ct) { Store[c.Id] = c; return Task.CompletedTask; }
        public void AddModule(Module module) { }
        public void AddLesson(Lesson lesson) { }
        public Task<Course?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Store.GetValueOrDefault(id));
        public Task<IReadOnlyList<Course>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Course>>([.. Store.Values.Where(c => ids.Contains(c.Id))]);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUser(Guid id) : ICurrentUser
    {
        public Guid UserId => id;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> RoleCodes => ["learner"];
        public Guid? CurrentDepartmentId => null;
    }
    private sealed class FakeTenant : ITenantContext { public Guid OrganizationId => OrgId; }

    private sealed record Scenario(FakeEnrollmentRepo Enrollments, FakePublicationRepo Pubs, FakeCourseRepo Courses, Enrollment Enrollment, Guid Lesson1, Guid Lesson2);

    private static Scenario Build(Guid learnerId)
    {
        var courses = new FakeCourseRepo();
        var pubs = new FakePublicationRepo();
        var enrollmentsRepo = new FakeEnrollmentRepo();

        var course = Course.Create(Guid.NewGuid(), OrgId, "Customer Service", "cs-101");
        var module = course.AddModule(Guid.NewGuid(), "Intro", DateTimeOffset.UtcNow);
        var l1 = course.AddLesson(module.Id, Guid.NewGuid(), "Welcome", "https://cdn.example.com/a.mp4", 60, DateTimeOffset.UtcNow);
        var l2 = course.AddLesson(module.Id, Guid.NewGuid(), "Next", "https://cdn.example.com/b.mp4", 60, DateTimeOffset.UtcNow);
        courses.Store[course.Id] = course;

        var pub = Publication.CreateForCourse(Guid.NewGuid(), OrgId, course.Id, "Publish CS");
        pub.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        pubs.Store[pub.Id] = pub;

        var enrollment = Enrollment.CreateAssigned(Guid.NewGuid(), OrgId, learnerId, pub.Id, [l1.Id, l2.Id], DateTimeOffset.UtcNow);
        enrollmentsRepo.Store[enrollment.Id] = enrollment;

        return new Scenario(enrollmentsRepo, pubs, courses, enrollment, l1.Id, l2.Id);
    }

    private static GetMyEnrollmentsHandler MyEnrollments(Scenario s, Guid caller)
        => new(s.Enrollments, s.Pubs, s.Courses, new FakeUser(caller), new FakeTenant());

    private static GetEnrolledCourseDetailHandler Detail(Scenario s, Guid caller)
        => new(s.Enrollments, s.Pubs, s.Courses, new FakeUser(caller), new FakeTenant());

    private static CompleteLessonHandler Complete(Scenario s, Guid caller)
        => new(s.Enrollments, new FakeUser(caller), new FakeTenant());

    [Fact]
    public async Task GetMyEnrollments_returns_only_callers_with_course_title()
    {
        var s = Build(LearnerId);

        var result = await MyEnrollments(s, LearnerId).Handle(new GetMyEnrollmentsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("Customer Service", result.Value[0].CourseTitle);

        // a different caller sees none of this learner's enrollments
        var other = await MyEnrollments(s, OtherUserId).Handle(new GetMyEnrollmentsQuery(), CancellationToken.None);
        Assert.Empty(other.Value);
    }

    [Fact]
    public async Task GetEnrolledCourseDetail_for_own_enrollment_lists_lessons()
    {
        var s = Build(LearnerId);

        var result = await Detail(s, LearnerId).Handle(new GetEnrolledCourseDetailQuery(s.Enrollment.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Modules);
        Assert.Equal(2, result.Value.Modules[0].Lessons.Count);
        Assert.All(result.Value.Modules[0].Lessons, l => Assert.False(l.Completed));
    }

    [Fact]
    public async Task GetEnrolledCourseDetail_for_another_users_enrollment_is_not_found()
    {
        var s = Build(LearnerId);

        var result = await Detail(s, OtherUserId).Handle(new GetEnrolledCourseDetailQuery(s.Enrollment.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CompleteLesson_advances_progress_and_marks_completed_lesson()
    {
        var s = Build(LearnerId);

        var result = await Complete(s, LearnerId).Handle(new CompleteLessonCommand(s.Enrollment.Id, s.Lesson1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.ProgressPercent);
        Assert.Equal("InProgress", result.Value.Status);

        var detail = await Detail(s, LearnerId).Handle(new GetEnrolledCourseDetailQuery(s.Enrollment.Id), CancellationToken.None);
        Assert.True(detail.Value.Modules[0].Lessons.Single(l => l.Id == s.Lesson1).Completed);
    }

    [Fact]
    public async Task CompleteLesson_all_completes_enrollment()
    {
        var s = Build(LearnerId);

        await Complete(s, LearnerId).Handle(new CompleteLessonCommand(s.Enrollment.Id, s.Lesson1), CancellationToken.None);
        var result = await Complete(s, LearnerId).Handle(new CompleteLessonCommand(s.Enrollment.Id, s.Lesson2), CancellationToken.None);

        Assert.Equal(100, result.Value.ProgressPercent);
        Assert.Equal("Completed", result.Value.Status);
    }

    [Fact]
    public async Task CompleteLesson_is_idempotent()
    {
        var s = Build(LearnerId);

        await Complete(s, LearnerId).Handle(new CompleteLessonCommand(s.Enrollment.Id, s.Lesson1), CancellationToken.None);
        var again = await Complete(s, LearnerId).Handle(new CompleteLessonCommand(s.Enrollment.Id, s.Lesson1), CancellationToken.None);

        Assert.True(again.IsSuccess);
        Assert.Equal(50, again.Value.ProgressPercent);
    }

    [Fact]
    public async Task CompleteLesson_on_another_users_enrollment_is_not_found()
    {
        var s = Build(LearnerId);

        var result = await Complete(s, OtherUserId).Handle(new CompleteLessonCommand(s.Enrollment.Id, s.Lesson1), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CompleteLesson_with_lesson_not_in_enrollment_is_not_found()
    {
        var s = Build(LearnerId);

        var result = await Complete(s, LearnerId).Handle(new CompleteLessonCommand(s.Enrollment.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
