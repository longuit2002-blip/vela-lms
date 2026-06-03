using Lms.Infrastructure.Security;

namespace Lms.Infrastructure.UnitTests;

public class RefreshTokenHasherTests
{
    private readonly RefreshTokenHasher _hasher = new();

    [Fact]
    public void Generate_returns_distinct_raw_tokens_whose_hash_matches()
    {
        var a = _hasher.Generate();
        var b = _hasher.Generate();

        Assert.NotEqual(a.RawToken, b.RawToken);
        Assert.Equal(a.TokenHash, _hasher.Hash(a.RawToken));
        Assert.Equal(b.TokenHash, _hasher.Hash(b.RawToken));
    }

    [Fact]
    public void Hash_is_deterministic_and_fixed_length_hex()
    {
        var hash1 = _hasher.Hash("token-abc");
        var hash2 = _hasher.Hash("token-abc");

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA-256 hex
        Assert.NotEqual(_hasher.Hash("token-abc"), _hasher.Hash("token-abd"));
    }

    [Fact]
    public void Raw_token_is_url_safe()
    {
        var token = _hasher.Generate().RawToken;

        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
    }
}
