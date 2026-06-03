using Lms.Domain.Organizations;

namespace Lms.Domain.UnitTests;

public class OrganizationTests
{
    [Fact]
    public void Create_WithValidValues_TrimsNameNormalizesSlugAndDefaultsToActive()
    {
        var id = Guid.NewGuid();

        var org = Organization.Create(id, "  Acme Corp  ", "Acme-Corp");

        Assert.Equal(id, org.Id);
        Assert.Equal("Acme Corp", org.Name);
        Assert.Equal("acme-corp", org.Slug);
        Assert.Equal(OrganizationStatus.Active, org.Status);
        Assert.NotEqual(default, org.CreatedAt);
        Assert.Equal(org.CreatedAt, org.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => Organization.Create(Guid.NewGuid(), name, "valid-slug"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has space")]
    [InlineData("bad_slug")]
    [InlineData("dot.dot")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    public void Create_WithInvalidSlug_Throws(string slug)
    {
        Assert.Throws<ArgumentException>(() => Organization.Create(Guid.NewGuid(), "Acme", slug));
    }

    [Fact]
    public void Create_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => Organization.Create(Guid.Empty, "Acme", "acme"));
    }

    [Fact]
    public void Entities_WithSameId_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = Organization.Create(id, "Acme", "acme");
        var b = Organization.Create(id, "Other", "other");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
