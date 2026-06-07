using Lms.Application.Abstractions;
using Lms.Application.Authorization;

namespace Lms.Application.UnitTests;

public class DepartmentBranchGuardTests
{
    private sealed class FakeCurrentUser(Guid? departmentId, params string[] roleCodes) : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> RoleCodes { get; } = roleCodes;
        public Guid? CurrentDepartmentId { get; } = departmentId;
    }

    private sealed class FakeResolver(params string[] granted) : IPermissionResolver
    {
        public Task<IReadOnlySet<string>> ResolveAsync(Guid userId, IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(granted, StringComparer.Ordinal));
    }

    private sealed class FakeClosure(bool inBranch) : IDepartmentClosure
    {
        public Task<bool> IsInBranchAsync(Guid ancestor, Guid descendant, CancellationToken cancellationToken)
            => Task.FromResult(inBranch);
    }

    private static DepartmentBranchGuard Guard(ICurrentUser user, IPermissionResolver resolver, bool inBranch)
        => new(user, resolver, new FakeClosure(inBranch));

    [Fact]
    public async Task Full_scope_caller_skips_the_branch_check()
    {
        // OrgOwner/OrgAdmin hold departments.manage.all — allowed even when closure says out-of-branch.
        var guard = Guard(
            new FakeCurrentUser(departmentId: null, "OrgOwner"),
            new FakeResolver(Permissions.Departments.Manage, Permissions.Departments.ManageAll),
            inBranch: false);

        await guard.EnsureCanManageAsync(Guid.NewGuid(), CancellationToken.None); // does not throw
    }

    [Fact]
    public async Task Branch_limited_caller_allowed_inside_own_subtree()
    {
        var guard = Guard(
            new FakeCurrentUser(Guid.NewGuid(), "LndManager"),
            new FakeResolver(Permissions.Departments.Manage),
            inBranch: true);

        await guard.EnsureCanManageAsync(Guid.NewGuid(), CancellationToken.None); // does not throw
    }

    [Fact]
    public async Task Branch_limited_caller_denied_outside_branch()
    {
        var guard = Guard(
            new FakeCurrentUser(Guid.NewGuid(), "LndManager"),
            new FakeResolver(Permissions.Departments.Manage),
            inBranch: false);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            guard.EnsureCanManageAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Branch_limited_caller_with_no_department_is_denied_fail_closed()
    {
        // Even though the closure would say "in branch", a null department scope must deny.
        var guard = Guard(
            new FakeCurrentUser(departmentId: null, "LndManager"),
            new FakeResolver(Permissions.Departments.Manage),
            inBranch: true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            guard.EnsureCanManageAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
