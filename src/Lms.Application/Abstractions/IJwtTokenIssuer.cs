using Lms.Domain.Users;

namespace Lms.Application.Abstractions;

/// <summary>
/// Issues signed access tokens (RS256) for a user. Implemented in Infrastructure. Claims always
/// carry <c>sub</c>/<c>org</c>/<c>roles</c>; roles are re-sourced from the user on every issue.
/// </summary>
public interface IJwtTokenIssuer
{
    AccessToken Issue(User user, DateTimeOffset now);
}

/// <summary>An issued access token and its absolute expiry.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
