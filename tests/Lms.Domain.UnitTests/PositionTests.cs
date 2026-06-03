using Lms.Domain.Positions;

namespace Lms.Domain.UnitTests;

public class PositionTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    [Fact]
    public void Create_sets_fields_and_trims_name()
    {
        var id = Guid.NewGuid();
        var position = Position.Create(id, OrgId, "  Agent  ");

        Assert.Equal(id, position.Id);
        Assert.Equal(OrgId, position.OrganizationId);
        Assert.Equal("Agent", position.Name);
        Assert.Equal(position.CreatedAt, position.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_name(string name) =>
        Assert.Throws<ArgumentException>(() => Position.Create(Guid.NewGuid(), OrgId, name));

    [Fact]
    public void Create_rejects_empty_org_and_empty_id()
    {
        Assert.Throws<ArgumentException>(() => Position.Create(Guid.NewGuid(), Guid.Empty, "Agent"));
        Assert.Throws<ArgumentException>(() => Position.Create(Guid.Empty, OrgId, "Agent"));
    }

    [Fact]
    public void Rename_updates_name_and_bumps_timestamp()
    {
        var position = Position.Create(Guid.NewGuid(), OrgId, "Agent");
        var later = position.UpdatedAt.AddMinutes(5);

        position.Rename("Senior Agent", later);

        Assert.Equal("Senior Agent", position.Name);
        Assert.Equal(later, position.UpdatedAt);
    }
}
