using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lms.Infrastructure.Security;

/// <summary>
/// Owns the RSA key used to sign (and, for this single self-issuing API, validate) access tokens.
/// Loads a PEM private key from <see cref="JwtOptions.PrivateKeyPem"/> when configured; otherwise
/// generates an ephemeral 2048-bit key for development. Registered as a singleton so the issuer and
/// the JwtBearer validation share one key. A JWKS endpoint + rotation are deferred (see plan).
/// </summary>
public sealed class RsaKeyProvider : IDisposable
{
    private readonly RSA _rsa;

    public RsaSecurityKey SecurityKey { get; }

    public RsaKeyProvider(IOptions<JwtOptions> options)
    {
        var jwt = options.Value;
        _rsa = RSA.Create();
        if (!string.IsNullOrWhiteSpace(jwt.PrivateKeyPem))
            _rsa.ImportFromPem(jwt.PrivateKeyPem);
        else
            _rsa.KeySize = 2048; // ephemeral dev key (regenerated each start)

        SecurityKey = new RsaSecurityKey(_rsa) { KeyId = jwt.KeyId };
    }

    public SigningCredentials SigningCredentials => new(SecurityKey, SecurityAlgorithms.RsaSha256);

    public void Dispose() => _rsa.Dispose();
}
