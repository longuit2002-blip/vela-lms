using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Positions.Commands.CreatePosition;
using Lms.Application.Positions.Commands.DeletePosition;
using Lms.Application.Positions.Commands.RenamePosition;
using Lms.Domain.Positions;

namespace Lms.Application.UnitTests;

public class PositionHandlersTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private sealed class FakePositionRepository : IPositionRepository
    {
        public Dictionary<Guid, Position> Store { get; } = new();
        public HashSet<string> ExistingNames { get; } = new(StringComparer.Ordinal);
        public bool HasUsers { get; set; }
        public bool Removed { get; private set; }

        public Task AddAsync(Position position, CancellationToken ct) { Store[position.Id] = position; ExistingNames.Add(position.Name); return Task.CompletedTask; }
        public Task<Position?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Store.GetValueOrDefault(id));
        public Task<IReadOnlyList<Position>> ListByOrganizationAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Position>>([.. Store.Values]);
        public Task<bool> NameExistsAsync(Guid organizationId, string name, CancellationToken ct) => Task.FromResult(ExistingNames.Contains(name));
        public Task<bool> HasAssignedUsersAsync(Guid positionId, CancellationToken ct) => Task.FromResult(HasUsers);
        public Task RemoveAsync(Position position, CancellationToken ct) { Removed = true; Store.Remove(position.Id); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeTenant : ITenantContext
    {
        public Guid OrganizationId => OrgId;
    }

    private sealed class FakeIdGenerator : IIdGenerator
    {
        public Guid NewId() => Guid.NewGuid();
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_name()
    {
        var repo = new FakePositionRepository();
        repo.ExistingNames.Add("Agent");

        var result = await new CreatePositionHandler(repo, new FakeIdGenerator(), new FakeTenant())
            .Handle(new CreatePositionCommand("Agent"), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Create_succeeds_for_a_new_name()
    {
        var repo = new FakePositionRepository();

        var result = await new CreatePositionHandler(repo, new FakeIdGenerator(), new FakeTenant())
            .Handle(new CreatePositionCommand("  Manager  "), CancellationToken.None);

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal("Manager", result.Value.Name);
        Assert.Single(repo.Store);
    }

    [Fact]
    public async Task Delete_blocks_when_a_user_holds_the_position()
    {
        var repo = new FakePositionRepository { HasUsers = true };
        var position = Position.Create(Guid.NewGuid(), OrgId, "Agent");
        repo.Store[position.Id] = position;

        var result = await new DeletePositionHandler(repo).Handle(new DeletePositionCommand(position.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.False(repo.Removed);
    }

    [Fact]
    public async Task Delete_succeeds_for_an_unused_position()
    {
        var repo = new FakePositionRepository();
        var position = Position.Create(Guid.NewGuid(), OrgId, "Agent");
        repo.Store[position.Id] = position;

        var result = await new DeletePositionHandler(repo).Handle(new DeletePositionCommand(position.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repo.Removed);
    }

    [Fact]
    public async Task Delete_returns_not_found_for_a_missing_position()
    {
        var result = await new DeletePositionHandler(new FakePositionRepository())
            .Handle(new DeletePositionCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Rename_rejects_a_clash_with_a_different_position()
    {
        var repo = new FakePositionRepository();
        var position = Position.Create(Guid.NewGuid(), OrgId, "Agent");
        repo.Store[position.Id] = position;
        repo.ExistingNames.Add("Agent");
        repo.ExistingNames.Add("Manager");

        var result = await new RenamePositionHandler(repo)
            .Handle(new RenamePositionCommand(position.Id, "Manager"), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }
}
