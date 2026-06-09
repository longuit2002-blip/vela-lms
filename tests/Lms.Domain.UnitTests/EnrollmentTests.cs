using Lms.Domain.Learning;

namespace Lms.Domain.UnitTests;

public class EnrollmentTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PubId = Guid.NewGuid();

    private static Enrollment Assigned(params Guid[] lessonIds) =>
        Enrollment.CreateAssigned(Guid.NewGuid(), OrgId, UserId, PubId, lessonIds, DateTimeOffset.UtcNow);

    [Fact]
    public void CreateAssigned_seeds_progress_and_starts_not_started()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var e = Assigned(l1, l2);

        Assert.Equal(OrgId, e.OrganizationId);
        Assert.Equal(UserId, e.UserId);
        Assert.Equal(PubId, e.PublicationId);
        Assert.Equal(EnrollmentSource.Assigned, e.Source);
        Assert.Equal(EnrollmentStatus.NotStarted, e.Status);
        Assert.Equal(0, e.ProgressPercent);
        Assert.Null(e.StartedAt);
        Assert.Null(e.CompletedAt);
        Assert.Equal(2, e.LessonProgress.Count);
        Assert.All(e.LessonProgress, p => Assert.Equal(LessonProgressStatus.NotStarted, p.Status));
    }

    [Fact]
    public void CreateAssigned_rejects_empty_lesson_set()
    {
        Assert.Throws<ArgumentException>(() => Assigned());
    }

    [Fact]
    public void CreateAssigned_rejects_empty_ids()
    {
        Assert.Throws<ArgumentException>(() =>
            Enrollment.CreateAssigned(Guid.Empty, OrgId, UserId, PubId, new[] { Guid.NewGuid() }, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            Enrollment.CreateAssigned(Guid.NewGuid(), Guid.Empty, UserId, PubId, new[] { Guid.NewGuid() }, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            Enrollment.CreateAssigned(Guid.NewGuid(), OrgId, Guid.Empty, PubId, new[] { Guid.NewGuid() }, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            Enrollment.CreateAssigned(Guid.NewGuid(), OrgId, UserId, Guid.Empty, new[] { Guid.NewGuid() }, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CompleteLesson_one_of_two_is_in_progress_at_fifty_percent()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var e = Assigned(l1, l2);
        var now = DateTimeOffset.UtcNow.AddMinutes(1);

        e.CompleteLesson(l1, now);

        Assert.Equal(50, e.ProgressPercent);
        Assert.Equal(EnrollmentStatus.InProgress, e.Status);
        Assert.Equal(now, e.StartedAt);
        Assert.Null(e.CompletedAt);
        Assert.Equal(LessonProgressStatus.Completed, e.LessonProgress.Single(p => p.LessonId == l1).Status);
        Assert.Equal(LessonProgressStatus.NotStarted, e.LessonProgress.Single(p => p.LessonId == l2).Status);
    }

    [Fact]
    public void CompleteLesson_all_completes_enrollment()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var e = Assigned(l1, l2);
        var now = DateTimeOffset.UtcNow.AddMinutes(2);

        e.CompleteLesson(l1, now);
        e.CompleteLesson(l2, now);

        Assert.Equal(100, e.ProgressPercent);
        Assert.Equal(EnrollmentStatus.Completed, e.Status);
        Assert.Equal(now, e.CompletedAt);
    }

    [Fact]
    public void CompleteLesson_is_idempotent_on_already_completed()
    {
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var e = Assigned(l1, l2);
        var now = DateTimeOffset.UtcNow;

        e.CompleteLesson(l1, now);
        var before = e.ProgressPercent;
        e.CompleteLesson(l1, now.AddMinutes(5)); // re-complete same lesson

        Assert.Equal(before, e.ProgressPercent);
        Assert.Equal(EnrollmentStatus.InProgress, e.Status);
    }

    [Fact]
    public void CompleteLesson_unknown_lesson_throws()
    {
        var e = Assigned(Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => e.CompleteLesson(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
}
