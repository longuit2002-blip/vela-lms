using Lms.Infrastructure.Security;

namespace Lms.Infrastructure.UnitTests;

public class Argon2idPasswordHasherTests
{
    private readonly Argon2idPasswordHasher _hasher = new();

    [Fact]
    public void Hash_produces_phc_string_and_verifies()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.StartsWith("$argon2id$v=19$m=19456,t=2,p=1$", hash);

        var result = _hasher.Verify(hash, "correct horse battery staple");
        Assert.True(result.Succeeded);
        Assert.False(result.NeedsRehash);
    }

    [Fact]
    public void Hash_is_salted_so_two_hashes_of_same_password_differ()
    {
        var a = _hasher.Hash("same-password");
        var b = _hasher.Hash("same-password");

        Assert.NotEqual(a, b);
        Assert.True(_hasher.Verify(a, "same-password").Succeeded);
        Assert.True(_hasher.Verify(b, "same-password").Succeeded);
    }

    [Fact]
    public void Verify_fails_for_wrong_password()
    {
        var hash = _hasher.Hash("right");

        Assert.False(_hasher.Verify(hash, "wrong").Succeeded);
    }

    [Theory]
    [InlineData("not-a-phc-string")]
    [InlineData("$argon2id$v=19$m=19456,t=2,p=1$only-one-segment")]
    [InlineData("")]
    public void Verify_fails_gracefully_for_malformed_hash(string malformed)
    {
        var result = _hasher.Verify(malformed, "whatever");

        Assert.False(result.Succeeded);
        Assert.False(result.NeedsRehash);
    }

    [Fact]
    public void Verify_flags_rehash_when_stored_params_are_weaker_than_policy()
    {
        // A correct hash produced with weaker parameters (m below current policy) should verify
        // but request a rehash. Build it via the public Hash then rewrite the param header with a
        // matching recomputation: simplest is to assert against a hand-built weak hash.
        var weakHash = BuildWeakHash("pw", memoryKiB: 8192, iterations: 2, parallelism: 1);

        var result = _hasher.Verify(weakHash, "pw");

        Assert.True(result.Succeeded);
        Assert.True(result.NeedsRehash);
    }

    // Reproduces the hasher's PHC encoding with weaker parameters, for the rehash-detection test.
    private static string BuildWeakHash(string password, int memoryKiB, int iterations, int parallelism)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        using var argon2 = new Konscious.Security.Cryptography.Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKiB,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };
        var hash = argon2.GetBytes(32);
        return $"$argon2id$v=19$m={memoryKiB},t={iterations},p={parallelism}$" +
               $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
