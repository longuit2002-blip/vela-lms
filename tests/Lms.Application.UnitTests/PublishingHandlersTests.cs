using Ardalis.Result;
using FluentValidation;
using Lms.Application.Abstractions;
using Lms.Application.Publishing.Commands.AssignPublication;
using Lms.Application.Publishing.Commands.CreatePublication;
using Lms.Application.Publishing.Commands.PublishPublication;
using Lms.Domain.Courses;
using Lms.Domain.Learning;
using Lms.Domain.Publishing;
using Lms.Domain.Users;

namespace Lms.Application.UnitTests;

public class PublishingHandlersTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid PublisherId = Guid.NewGuid();

    private sealed class FakeCourseRepo : ICourseRepository
    {
        public Dictionary<Guid, Course> Store { get; } = new();
        public Task AddAsync(Course c, CancellationToken ct) { Store[c.Id] = c; return Task.CompletedTask; }
        public Task<Course?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Store.GetValueOrDefault(id));
        public Task<IReadOnlyList<Course>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Course>>([.. Store.Values.Where(c => ids.Contains(c.Id))]);
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

    private sealed class FakeUserRepo : IUserRepository
    {
        public HashSet<Guid> Existing { get; } = new();
        private readonly User _any = User.Create(Guid.NewGuid(), OrgId, "u@x.com", "hash", ["learner"], false);
        public Task AddAsync(User u, CancellationToken ct) => Task.CompletedTask;
        public Task<User?> FindByEmailForLoginAsync(string e, CancellationToken ct) => Task.FromResult<User?>(null);
        public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Existing.Contains(id) ? _any : null);
        public Task<User?> FindByIdForTokenIssueAsync(Guid id, CancellationToken ct) => Task.FromResult<User?>(null);
        public Task<bool> EmailExistsAsync(Guid org, string e, CancellationToken ct) => Task.FromResult(false);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeEnrollmentRepo : IEnrollmentRepository
    {
        public HashSet<(Guid user, Guid pub)> ExistingPairs { get; } = new();
        public List<Enrollment> Added { get; } = new();
        public Task AddRangeAsync(IEnumerable<Enrollment> e, CancellationToken ct) { Added.AddRange(e); return Task.CompletedTask; }
        public Task<Enrollment?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Enrollment?>(null);
        public Task<IReadOnlyList<Enrollment>> ListByUserAsync(Guid u, CancellationToken ct) => Task.FromResult<IReadOnlyList<Enrollment>>([]);
        public Task<bool> ExistsAsync(Guid u, Guid p, CancellationToken ct) => Task.FromResult(ExistingPairs.Contains((u, p)));
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeTenant : ITenantContext { public Guid OrganizationId => OrgId; }
    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => PublisherId;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> RoleCodes => ["lnd_manager"];
        public Guid? CurrentDepartmentId => null;
    }
    private sealed class FakeIdGen : IIdGenerator { public Guid NewId() => Guid.NewGuid(); }

    private static Course CourseWithLesson()
    {
        var course = Course.Create(Guid.NewGuid(), OrgId, "C", "c");
        var m = course.AddModule(Guid.NewGuid(), "M", DateTimeOffset.UtcNow);
        course.AddLesson(m.Id, Guid.NewGuid(), "L", "https://cdn.example.com/a.mp4", 60, DateTimeOffset.UtcNow);
        return course;
    }

    [Fact]
    public async Task CreatePublication_for_existing_course_creates_draft()
    {
        var courses = new FakeCourseRepo();
        var pubs = new FakePublicationRepo();
        var course = CourseWithLesson();
        courses.Store[course.Id] = course;

        var result = await new CreatePublicationHandler(pubs, courses, new FakeIdGen(), new FakeTenant())
            .Handle(new CreatePublicationCommand(course.Id, "Publish CS"), CancellationToken.None);

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal("course", result.Value.Kind);
        Assert.Equal("Draft", result.Value.Status);
    }

    [Fact]
    public async Task CreatePublication_for_missing_course_is_not_found()
    {
        var result = await new CreatePublicationHandler(new FakePublicationRepo(), new FakeCourseRepo(), new FakeIdGen(), new FakeTenant())
            .Handle(new CreatePublicationCommand(Guid.NewGuid(), "T"), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Publish_course_with_no_lessons_throws_validation()
    {
        var courses = new FakeCourseRepo();
        var pubs = new FakePublicationRepo();
        var course = Course.Create(Guid.NewGuid(), OrgId, "Empty", "empty"); // no lessons
        courses.Store[course.Id] = course;
        var pub = Publication.CreateForCourse(Guid.NewGuid(), OrgId, course.Id, "P");
        pubs.Store[pub.Id] = pub;

        var handler = new PublishPublicationHandler(pubs, courses, new FakeCurrentUser(), new FakeTenant());

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new PublishPublicationCommand(pub.Id), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Publish_course_with_lessons_succeeds()
    {
        var courses = new FakeCourseRepo();
        var pubs = new FakePublicationRepo();
        var course = CourseWithLesson();
        courses.Store[course.Id] = course;
        var pub = Publication.CreateForCourse(Guid.NewGuid(), OrgId, course.Id, "P");
        pubs.Store[pub.Id] = pub;

        var result = await new PublishPublicationHandler(pubs, courses, new FakeCurrentUser(), new FakeTenant())
            .Handle(new PublishPublicationCommand(pub.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Published", result.Value.Status);
    }

    [Fact]
    public async Task Publish_when_not_draft_is_conflict()
    {
        var courses = new FakeCourseRepo();
        var pubs = new FakePublicationRepo();
        var course = CourseWithLesson();
        courses.Store[course.Id] = course;
        var pub = Publication.CreateForCourse(Guid.NewGuid(), OrgId, course.Id, "P");
        pub.Publish(PublisherId, DateTimeOffset.UtcNow);
        pubs.Store[pub.Id] = pub;

        var result = await new PublishPublicationHandler(pubs, courses, new FakeCurrentUser(), new FakeTenant())
            .Handle(new PublishPublicationCommand(pub.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    private static (FakePublicationRepo pubs, FakeCourseRepo courses, Publication pub) PublishedSetup()
    {
        var courses = new FakeCourseRepo();
        var pubs = new FakePublicationRepo();
        var course = CourseWithLesson();
        courses.Store[course.Id] = course;
        var pub = Publication.CreateForCourse(Guid.NewGuid(), OrgId, course.Id, "P");
        pub.Publish(PublisherId, DateTimeOffset.UtcNow);
        pubs.Store[pub.Id] = pub;
        return (pubs, courses, pub);
    }

    [Fact]
    public async Task Assign_on_draft_is_conflict()
    {
        var courses = new FakeCourseRepo();
        var pubs = new FakePublicationRepo();
        var course = CourseWithLesson();
        courses.Store[course.Id] = course;
        var pub = Publication.CreateForCourse(Guid.NewGuid(), OrgId, course.Id, "P"); // draft
        pubs.Store[pub.Id] = pub;

        var result = await new AssignPublicationHandler(pubs, courses, new FakeUserRepo(), new FakeEnrollmentRepo(), new FakeIdGen(), new FakeTenant())
            .Handle(new AssignPublicationCommand(pub.Id, [Guid.NewGuid()]), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Assign_to_valid_users_creates_enrollments()
    {
        var (pubs, courses, pub) = PublishedSetup();
        var users = new FakeUserRepo();
        var enrollments = new FakeEnrollmentRepo();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        users.Existing.Add(u1);
        users.Existing.Add(u2);

        var result = await new AssignPublicationHandler(pubs, courses, users, enrollments, new FakeIdGen(), new FakeTenant())
            .Handle(new AssignPublicationCommand(pub.Id, [u1, u2]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Enrolled);
        Assert.Equal(2, enrollments.Added.Count);
    }

    [Fact]
    public async Task Assign_with_an_unknown_user_is_not_found_and_creates_nothing()
    {
        var (pubs, courses, pub) = PublishedSetup();
        var users = new FakeUserRepo();
        var enrollments = new FakeEnrollmentRepo();
        var u1 = Guid.NewGuid();
        users.Existing.Add(u1); // u2 is unknown

        var result = await new AssignPublicationHandler(pubs, courses, users, enrollments, new FakeIdGen(), new FakeTenant())
            .Handle(new AssignPublicationCommand(pub.Id, [u1, Guid.NewGuid()]), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Empty(enrollments.Added);
    }

    [Fact]
    public async Task Assign_skips_already_enrolled_users()
    {
        var (pubs, courses, pub) = PublishedSetup();
        var users = new FakeUserRepo();
        var enrollments = new FakeEnrollmentRepo();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        users.Existing.Add(u1);
        users.Existing.Add(u2);
        enrollments.ExistingPairs.Add((u1, pub.Id)); // u1 already enrolled

        var result = await new AssignPublicationHandler(pubs, courses, users, enrollments, new FakeIdGen(), new FakeTenant())
            .Handle(new AssignPublicationCommand(pub.Id, [u1, u2]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Enrolled);
        Assert.Equal(1, result.Value.Skipped);
        Assert.Single(enrollments.Added);
    }
}
