using Lms.Application.Abstractions;
using Lms.Domain.Roles;
using Lms.Infrastructure.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace Lms.Infrastructure.UnitTests;

public class PermissionResolverTests
{
    private sealed class FakeRoleRepository(params Role[] roles) : IRoleRepository
    {
        public int GetSystemRolesCalls { get; private set; }

        public Task AddAsync(Role role, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> SystemRoleExistsAsync(string code, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken)
        {
            GetSystemRolesCalls++;
            return Task.FromResult<IReadOnlyList<Role>>(roles);
        }
    }

    private static (PermissionResolver resolver, FakeRoleRepository repo) Build(params Role[] roles)
    {
        var repo = new FakeRoleRepository(roles);
        return (new PermissionResolver(repo, new MemoryCache(new MemoryCacheOptions())), repo);
    }

    [Fact]
    public async Task Unions_permissions_across_a_users_roles()
    {
        var (resolver, _) = Build(
            Role.CreateSystem(Guid.NewGuid(), "DeptManager", "Dept Manager", ["users.read", "users.update"]),
            Role.CreateSystem(Guid.NewGuid(), "Instructor", "Instructor", ["courses.create", "users.read"]));

        var granted = await resolver.ResolveAsync(Guid.NewGuid(), ["DeptManager", "Instructor"], CancellationToken.None);

        Assert.Contains("users.read", granted);
        Assert.Contains("users.update", granted);
        Assert.Contains("courses.create", granted);
        Assert.Equal(3, granted.Count);
    }

    [Fact]
    public async Task Ignores_unknown_role_codes_and_returns_empty_for_no_roles()
    {
        var (resolver, _) = Build(
            Role.CreateSystem(Guid.NewGuid(), "OrgOwner", "Owner", ["departments.manage.all"]));

        Assert.Empty(await resolver.ResolveAsync(Guid.NewGuid(), ["Nonexistent"], CancellationToken.None));
        Assert.Empty(await resolver.ResolveAsync(Guid.NewGuid(), [], CancellationToken.None));
    }

    [Fact]
    public async Task Caches_the_catalog_across_calls()
    {
        var (resolver, repo) = Build(
            Role.CreateSystem(Guid.NewGuid(), "OrgOwner", "Owner", ["departments.manage", "departments.manage.all"]));

        await resolver.ResolveAsync(Guid.NewGuid(), ["OrgOwner"], CancellationToken.None);
        await resolver.ResolveAsync(Guid.NewGuid(), ["OrgOwner"], CancellationToken.None);

        Assert.Equal(1, repo.GetSystemRolesCalls);
    }
}
