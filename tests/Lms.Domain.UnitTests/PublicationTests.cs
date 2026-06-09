using Lms.Domain.Publishing;

namespace Lms.Domain.UnitTests;

public class PublicationTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private static Publication NewDraft() =>
        Publication.CreateForCourse(Guid.NewGuid(), OrgId, Guid.NewGuid(), "  Customer Service 101  ");

    [Fact]
    public void CreateForCourse_sets_draft_and_canonical_kind()
    {
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var pub = Publication.CreateForCourse(id, OrgId, courseId, "  Customer Service 101  ");

        Assert.Equal(id, pub.Id);
        Assert.Equal(OrgId, pub.OrganizationId);
        Assert.Equal("course", pub.Kind);
        Assert.Equal(courseId, pub.ContentId);
        Assert.Equal("Customer Service 101", pub.Title);
        Assert.Equal(PublicationStatus.Draft, pub.Status);
        Assert.Null(pub.PublishedAt);
        Assert.Null(pub.PublishedBy);
    }

    [Fact]
    public void CreateForCourse_rejects_empty_ids_and_blank_title()
    {
        Assert.Throws<ArgumentException>(() => Publication.CreateForCourse(Guid.Empty, OrgId, Guid.NewGuid(), "T"));
        Assert.Throws<ArgumentException>(() => Publication.CreateForCourse(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "T"));
        Assert.Throws<ArgumentException>(() => Publication.CreateForCourse(Guid.NewGuid(), OrgId, Guid.Empty, "T"));
        Assert.Throws<ArgumentException>(() => Publication.CreateForCourse(Guid.NewGuid(), OrgId, Guid.NewGuid(), "  "));
    }

    [Fact]
    public void Publish_flips_draft_to_published_and_stamps()
    {
        var pub = NewDraft();
        var publisher = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        pub.Publish(publisher, now);

        Assert.Equal(PublicationStatus.Published, pub.Status);
        Assert.Equal(now, pub.PublishedAt);
        Assert.Equal(publisher, pub.PublishedBy);
    }

    [Fact]
    public void Publish_when_not_draft_throws()
    {
        var pub = NewDraft();
        pub.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => pub.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
}
