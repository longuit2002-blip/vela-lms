using System.Security.Claims;
using Lms.Application.Auth;
using Lms.Domain.Users;
using Lms.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lms.Infrastructure.UnitTests;

public class JwtTokenIssuerTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "lms",
        Audience = "lms",
        AccessTokenMinutes = 15,
        KeyId = "test",
    };

    private static readonly RsaKeyProvider Keys = new(Microsoft.Extensions.Options.Options.Create(Options));
    private readonly JwtTokenIssuer _issuer = new(Keys, Microsoft.Extensions.Options.Options.Create(Options));

    private static User NewUser() =>
        User.Create(Guid.NewGuid(), Guid.NewGuid(), "owner@vela.local", "$argon2id$h", ["OrgOwner", "Learner"], false);

    private static TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Options.Issuer,
        ValidAudience = Options.Audience,
        IssuerSigningKey = Keys.SecurityKey,
        ClockSkew = TimeSpan.FromMinutes(1),
    };

    [Fact]
    public async Task Issued_token_validates_and_carries_sub_org_roles()
    {
        var user = NewUser();
        var now = DateTimeOffset.UtcNow;

        var token = _issuer.Issue(user, now);
        Assert.True(token.ExpiresAt > now);

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token.Value, ValidationParameters());

        Assert.True(result.IsValid);
        var identity = result.ClaimsIdentity;
        Assert.Equal(user.Id.ToString(), identity.FindFirst("sub")?.Value);
        Assert.Equal(user.OrganizationId.ToString(), identity.FindFirst("org")?.Value);

        var roles = identity.FindAll("roles").Select(c => c.Value).ToList();
        Assert.Contains("OrgOwner", roles);
        Assert.Contains("Learner", roles);
    }

    [Fact]
    public async Task Expired_token_fails_validation_beyond_clock_skew()
    {
        var user = NewUser();
        var issuedInThePast = DateTimeOffset.UtcNow.AddMinutes(-20); // 15-min token, 1-min skew => expired

        var token = _issuer.Issue(user, issuedInThePast);

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token.Value, ValidationParameters());

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Token_signed_with_a_different_key_fails_validation()
    {
        var user = NewUser();
        var token = _issuer.Issue(user, DateTimeOffset.UtcNow);

        using var otherRsa = System.Security.Cryptography.RSA.Create(2048);
        var wrongParams = ValidationParameters();
        wrongParams.IssuerSigningKey = new RsaSecurityKey(otherRsa);

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token.Value, wrongParams);

        Assert.False(result.IsValid);
    }
}
