using Lms.Domain.Roles;

namespace Lms.Domain.UnitTests;

public class RoleTests
{
    [Fact]
    public void CreateSystem_is_org_null_and_marked_system()
    {
        var role = Role.CreateSystem(Guid.NewGuid(), "LndManager", "L&D Manager", ["departments.manage", "positions.manage"]);

        Assert.Null(role.OrganizationId);
        Assert.True(role.IsSystem);
        Assert.Equal("LndManager", role.Code);
        Assert.Equal("L&D Manager", role.Name);
        Assert.Contains("departments.manage", role.Permissions);
        Assert.Contains("positions.manage", role.Permissions);
    }

    [Fact]
    public void CreateSystem_trims_dedupes_and_drops_empty_permissions()
    {
        var role = Role.CreateSystem(Guid.NewGuid(), "Auditor", "Auditor",
            [" reports.read ", "reports.read", "", "  ", "audit.read"]);

        Assert.Equal(["reports.read", "audit.read"], role.Permissions);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateSystem_rejects_blank_code(string code) =>
        Assert.Throws<ArgumentException>(() => Role.CreateSystem(Guid.NewGuid(), code, "Name", []));

    [Fact]
    public void CreateSystem_rejects_blank_name_and_empty_id()
    {
        Assert.Throws<ArgumentException>(() => Role.CreateSystem(Guid.NewGuid(), "Code", "  ", []));
        Assert.Throws<ArgumentException>(() => Role.CreateSystem(Guid.Empty, "Code", "Name", []));
    }

    [Fact]
    public void CreateSystem_allows_empty_permission_set()
    {
        var role = Role.CreateSystem(Guid.NewGuid(), "Learner", "Learner", []);
        Assert.Empty(role.Permissions);
    }
}
