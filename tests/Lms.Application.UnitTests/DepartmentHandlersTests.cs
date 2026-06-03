using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Authorization;
using Lms.Application.Departments.Commands.DeleteDepartment;
using Lms.Application.Departments.Commands.MoveDepartment;
using Lms.Domain.Departments;

namespace Lms.Application.UnitTests;

public class DepartmentHandlersTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public Dictionary<Guid, Department> Store { get; } = new();
        public bool HasChildren { get; set; }
        public bool HasUsers { get; set; }
        public bool Removed { get; private set; }
        public bool Reparented { get; private set; }

        public Task AddAsync(Department department, CancellationToken ct) { Store[department.Id] = department; return Task.CompletedTask; }
        public Task<Department?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Store.GetValueOrDefault(id));
        public Task<IReadOnlyList<Department>> ListByOrganizationAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Department>>([.. Store.Values]);
        public Task ReparentAsync(Department department, CancellationToken ct) { Reparented = true; return Task.CompletedTask; }
        public Task RemoveAsync(Department department, CancellationToken ct) { Removed = true; Store.Remove(department.Id); return Task.CompletedTask; }
        public Task<bool> HasChildrenAsync(Guid id, CancellationToken ct) => Task.FromResult(HasChildren);
        public Task<bool> HasAssignedUsersAsync(Guid id, CancellationToken ct) => Task.FromResult(HasUsers);
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class AllowGuard : IDepartmentBranchGuard
    {
        public Task EnsureCanManageAsync(Guid targetDepartmentId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureFullScopeAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeClosure(bool inBranch) : IDepartmentClosure
    {
        public Task<bool> IsInBranchAsync(Guid ancestor, Guid descendant, CancellationToken ct) => Task.FromResult(inBranch);
    }

    private static Department NewDept() => Department.Create(Guid.NewGuid(), OrgId, null, "Dept");

    [Fact]
    public async Task Delete_blocks_when_department_has_children()
    {
        var repo = new FakeDepartmentRepository { HasChildren = true };
        var dept = NewDept();
        repo.Store[dept.Id] = dept;

        var result = await new DeleteDepartmentHandler(repo, new AllowGuard())
            .Handle(new DeleteDepartmentCommand(dept.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.False(repo.Removed);
    }

    [Fact]
    public async Task Delete_blocks_when_department_has_assigned_users()
    {
        var repo = new FakeDepartmentRepository { HasUsers = true };
        var dept = NewDept();
        repo.Store[dept.Id] = dept;

        var result = await new DeleteDepartmentHandler(repo, new AllowGuard())
            .Handle(new DeleteDepartmentCommand(dept.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.False(repo.Removed);
    }

    [Fact]
    public async Task Delete_succeeds_for_an_empty_leaf()
    {
        var repo = new FakeDepartmentRepository();
        var dept = NewDept();
        repo.Store[dept.Id] = dept;

        var result = await new DeleteDepartmentHandler(repo, new AllowGuard())
            .Handle(new DeleteDepartmentCommand(dept.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repo.Removed);
    }

    [Fact]
    public async Task Delete_returns_not_found_for_a_missing_department()
    {
        var result = await new DeleteDepartmentHandler(new FakeDepartmentRepository(), new AllowGuard())
            .Handle(new DeleteDepartmentCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Move_rejects_a_cycle_when_new_parent_is_in_the_subtree()
    {
        var repo = new FakeDepartmentRepository();
        var dept = NewDept();
        var newParent = NewDept();
        repo.Store[dept.Id] = dept;
        repo.Store[newParent.Id] = newParent;

        // closure says newParent is inside dept's subtree -> cycle.
        var result = await new MoveDepartmentHandler(repo, new AllowGuard(), new FakeClosure(inBranch: true))
            .Handle(new MoveDepartmentCommand(dept.Id, newParent.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.False(repo.Reparented);
    }

    [Fact]
    public async Task Move_succeeds_to_a_valid_new_parent()
    {
        var repo = new FakeDepartmentRepository();
        var dept = NewDept();
        var newParent = NewDept();
        repo.Store[dept.Id] = dept;
        repo.Store[newParent.Id] = newParent;

        var result = await new MoveDepartmentHandler(repo, new AllowGuard(), new FakeClosure(inBranch: false))
            .Handle(new MoveDepartmentCommand(dept.Id, newParent.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repo.Reparented);
        Assert.Equal(newParent.Id, dept.ParentId);
    }
}
