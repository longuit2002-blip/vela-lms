using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Organizations.Commands.CreateOrganization;
using Lms.Application.Organizations.Queries.GetMyOrganization;
using Lms.Domain.Organizations;

namespace Lms.Application.UnitTests;

public class OrganizationHandlersTests
{
    private sealed class FakeIdGenerator(Guid id) : IIdGenerator
    {
        public Guid NewId() => id;
    }

    private sealed class FakeTenantContext(Guid organizationId) : ITenantContext
    {
        public Guid OrganizationId => organizationId;
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

        public Task<Organization?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(o => o.Id == id));

        public Task<Organization?> FindBySlugAsync(string slug, CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(o => o.Slug == slug));

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
    public async Task GetMyOrganization_ReturnsCallersOwnOrg()
    {
        var orgId = Guid.NewGuid();
        var repo = new FakeOrganizationRepository();
        repo.Items.Add(Organization.Create(orgId, "Acme", "acme"));
        var handler = new GetMyOrganizationHandler(repo, new FakeTenantContext(orgId));

        var result = await handler.Handle(new GetMyOrganizationQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(orgId, result.Value.Id);
        Assert.Equal("acme", result.Value.Slug);
    }

    [Fact]
    public async Task GetMyOrganization_WithoutTenant_ReturnsUnauthorized()
    {
        var repo = new FakeOrganizationRepository();
        var handler = new GetMyOrganizationHandler(repo, new FakeTenantContext(Guid.Empty));

        var result = await handler.Handle(new GetMyOrganizationQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task GetMyOrganization_WhenOrgMissing_ReturnsNotFound()
    {
        var repo = new FakeOrganizationRepository();
        var handler = new GetMyOrganizationHandler(repo, new FakeTenantContext(Guid.NewGuid()));

        var result = await handler.Handle(new GetMyOrganizationQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
