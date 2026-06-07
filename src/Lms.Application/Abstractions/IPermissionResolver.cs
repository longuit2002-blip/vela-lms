namespace Lms.Application.Abstractions;

/// <summary>
/// Resolves a caller's effective permission codes server-side from their role codes, via the system
/// roles catalog (cached). Permission <em>definitions</em> are therefore always current (a corrected
/// role definition takes effect within the cache TTL, with no token reissue).
/// <para>
/// <paramref name="userId"/> is taken now even though resolution uses <paramref name="roleCodes"/>
/// from the token: when the members slice adds role reassignment, switching to a per-request DB read
/// of membership (full immediacy) becomes a pure implementation swap behind this port, with no
/// signature or caller change.
/// </para>
/// </summary>
public interface IPermissionResolver
{
    Task<IReadOnlySet<string>> ResolveAsync(Guid userId, IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken);
}
