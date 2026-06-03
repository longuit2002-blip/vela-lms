using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Organizations.Commands.CreateOrganization;
using Lms.Application.Organizations.Queries.ListOrganizations;
using Lms.Domain.Organizations;

namespace Lms.Application.UnitTests;

public class OrganizationHandlersTests
{
    private sealed class FakeIdGenerator(Guid id) : IIdGenerator
    {
        public Guid NewId() => id;
    }

    private sealed class FakeOrganizationRepository : IOrganizationRepository
    {
        public List<Organization> Items { get; } = [];
        public HashSet<string> ExistingSlugs { get; } = [];
        public int SaveCount { get; private set; }

        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
        {
            Items.Add(organization);
            return Task.CompletedTask;
        }

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
            => Task.FromResult(ExistingSlugs.Contains(slug));

        public Task<IReadOnlyList<Organization>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Organization>>(Items);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Create_WithValidCommand_PersistsAndReturnsCreatedWithMappedDto()
    {
        var id = Guid.NewGuid();
        var repo = new FakeOrganizationRepository();
        var handler = new CreateOrganizationHandler(repo, new FakeIdGenerator(id));

        var result = await handler.Handle(new CreateOrganizationCommand("Acme Corp", "Acme-Corp"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Acme Corp", result.Value.Name);
        Assert.Equal("acme-corp", result.Value.Slug);          // domain-normalized
        Assert.Equal("Active", result.Value.Status);
        Assert.Single(repo.Items);
        Assert.Equal(1, repo.SaveCount);
    }

    [Fact]
    public async Task Create_WhenSlugExists_ReturnsConflictAndDoesNotPersist()
    {
        var repo = new FakeOrganizationRepository();
        repo.ExistingSlugs.Add("acme-corp");
        var handler = new CreateOrganizationHandler(repo, new FakeIdGenerator(Guid.NewGuid()));

        var result = await handler.Handle(new CreateOrganizationCommand("Acme", "Acme-Corp"), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Empty(repo.Items);
        Assert.Equal(0, repo.SaveCount);
    }

    [Fact]
    public async Task List_ReturnsMappedDtosForAllOrganizations()
    {
        var repo = new FakeOrganizationRepository();
        repo.Items.Add(Organization.Create(Guid.NewGuid(), "Acme", "acme"));
        repo.Items.Add(Organization.Create(Guid.NewGuid(), "Globex", "globex"));
        var handler = new ListOrganizationsHandler(repo);

        var result = await handler.Handle(new ListOrganizationsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, d => d.Slug == "acme");
        Assert.Contains(result.Value, d => d.Slug == "globex");
    }
}
