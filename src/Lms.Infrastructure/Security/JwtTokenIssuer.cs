using Lms.Application.Abstractions;
using Lms.Application.Auth;
using Lms.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lms.Infrastructure.Security;

/// <summary>
/// Issues RS256 access tokens via <see cref="JsonWebTokenHandler"/>. Claims: <c>sub</c> (user id),
/// <c>org</c> (tenant id), <c>roles</c> (role codes, re-sourced from the user). Signed with the
/// shared <see cref="RsaKeyProvider"/> key.
/// </summary>
public sealed class JwtTokenIssuer(RsaKeyProvider keys, IOptions<JwtOptions> options) : IJwtTokenIssuer
{
    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    public AccessToken Issue(User user, DateTimeOffset now)
    {
        var jwt = options.Value;
        var expires = now.AddMinutes(jwt.AccessTokenMinutes);

        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["org"] = user.OrganizationId.ToString(),
            ["roles"] = user.RoleCodes.ToArray(),
        };

        // Forced-change gate reads this claim to block other actions until the password is changed.
        if (user.MustChangePassword)
            claims["mcp"] = true;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Claims = claims,
            SigningCredentials = keys.SigningCredentials,
        };

        return new AccessToken(Handler.CreateToken(descriptor), expires);
    }
}
