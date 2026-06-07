using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Authorization;
using Mediator;

namespace Lms.Application.UnitTests;

public class AuthorizationBehaviorTests
{
    private sealed record GuardedCommand(string Permission) : IRequest<Result<string>>, IRequirePermission;

    private sealed record UnguardedCommand : IRequest<Result<string>>;

    private sealed class FakeCurrentUser(bool authenticated, params string[] roleCodes) : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public bool IsAuthenticated { get; } = authenticated;
        public IReadOnlyCollection<string> RoleCodes { get; } = roleCodes;
        public Guid? CurrentDepartmentId => null;
    }

    private sealed class FakePermissionResolver(params string[] granted) : IPermissionResolver
    {
        public Task<IReadOnlySet<string>> ResolveAsync(Guid userId, IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(granted, StringComparer.Ordinal));
    }

    private static (MessageHandlerDelegate<TMessage, Result<string>> next, Func<bool> wasCalled) TrackingNext<TMessage>()
        where TMessage : notnull, IMessage
    {
        var called = false;
        MessageHandlerDelegate<TMessage, Result<string>> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success("ok"));
        };
        return (next, () => called);
    }

    [Fact]
    public async Task Calls_next_when_caller_has_the_required_permission()
    {
        var behavior = new AuthorizationBehavior<GuardedCommand, Result<string>>(
            new FakeCurrentUser(true, "LndManager"),
            new FakePermissionResolver("departments.manage", "positions.manage"));
        var (next, wasCalled) = TrackingNext<GuardedCommand>();

        var result = await behavior.Handle(new GuardedCommand("departments.manage"), next, CancellationToken.None);

        Assert.True(wasCalled());
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Throws_forbidden_and_skips_next_when_permission_absent()
    {
        var behavior = new AuthorizationBehavior<GuardedCommand, Result<string>>(
            new FakeCurrentUser(true, "Learner"),
            new FakePermissionResolver("learning.consume"));
        var (next, wasCalled) = TrackingNext<GuardedCommand>();

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await behavior.Handle(new GuardedCommand("departments.manage"), next, CancellationToken.None));

        Assert.False(wasCalled());
    }

    [Fact]
    public async Task Throws_forbidden_when_unauthenticated_even_if_resolver_would_grant()
    {
        var behavior = new AuthorizationBehavior<GuardedCommand, Result<string>>(
            new FakeCurrentUser(false, "OrgOwner"),
            new FakePermissionResolver("departments.manage"));
        var (next, wasCalled) = TrackingNext<GuardedCommand>();

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await behavior.Handle(new GuardedCommand("departments.manage"), next, CancellationToken.None));

        Assert.False(wasCalled());
    }

    [Fact]
    public async Task Passes_through_messages_without_a_permission_requirement()
    {
        var behavior = new AuthorizationBehavior<UnguardedCommand, Result<string>>(
            new FakeCurrentUser(false),
            new FakePermissionResolver());
        var (next, wasCalled) = TrackingNext<UnguardedCommand>();

        var result = await behavior.Handle(new UnguardedCommand(), next, CancellationToken.None);

        Assert.True(wasCalled());
        Assert.True(result.IsSuccess);
    }
}
