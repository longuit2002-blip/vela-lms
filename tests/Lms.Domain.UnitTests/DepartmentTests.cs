using Lms.Domain.Departments;

namespace Lms.Domain.UnitTests;

public class DepartmentTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    [Fact]
    public void Create_root_sets_fields_and_null_parent()
    {
        var id = Guid.NewGuid();
        var dept = Department.Create(id, OrgId, parentId: null, "  Sales  ");

        Assert.Equal(id, dept.Id);
        Assert.Equal(OrgId, dept.OrganizationId);
        Assert.Null(dept.ParentId);
        Assert.Equal("Sales", dept.Name);
        Assert.Equal(dept.CreatedAt, dept.UpdatedAt);
    }

    [Fact]
    public void Create_child_keeps_parent()
    {
        var parent = Guid.NewGuid();
        var dept = Department.Create(Guid.NewGuid(), OrgId, parent, "East");

        Assert.Equal(parent, dept.ParentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_name(string name) =>
        Assert.Throws<ArgumentException>(() => Department.Create(Guid.NewGuid(), OrgId, null, name));

    [Fact]
    public void Create_rejects_empty_org_and_empty_id()
    {
        Assert.Throws<ArgumentException>(() => Department.Create(Guid.NewGuid(), Guid.Empty, null, "Sales"));
        Assert.Throws<ArgumentException>(() => Department.Create(Guid.Empty, OrgId, null, "Sales"));
    }

    [Fact]
    public void Create_rejects_self_parent()
    {
        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => Department.Create(id, OrgId, parentId: id, "Sales"));
    }

    [Fact]
    public void Rename_updates_name_and_bumps_timestamp()
    {
        var dept = Department.Create(Guid.NewGuid(), OrgId, null, "Sales");
        var later = dept.UpdatedAt.AddMinutes(5);

        dept.Rename("  Revenue  ", later);

        Assert.Equal("Revenue", dept.Name);
        Assert.Equal(later, dept.UpdatedAt);
    }

    [Fact]
    public void Reparent_sets_new_parent_and_allows_null_root()
    {
        var dept = Department.Create(Guid.NewGuid(), OrgId, Guid.NewGuid(), "East");
        var newParent = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        dept.Reparent(newParent, now);
        Assert.Equal(newParent, dept.ParentId);

        dept.Reparent(null, now);
        Assert.Null(dept.ParentId);
    }

    [Fact]
    public void Reparent_rejects_self_parent()
    {
        var dept = Department.Create(Guid.NewGuid(), OrgId, null, "Sales");
        Assert.Throws<ArgumentException>(() => dept.Reparent(dept.Id, DateTimeOffset.UtcNow));
    }
}
