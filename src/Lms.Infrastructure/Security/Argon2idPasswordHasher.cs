using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Lms.Application.Abstractions;

namespace Lms.Infrastructure.Security;

/// <summary>
/// Argon2id password hasher (Konscious), encoding hashes in PHC string format
/// (<c>$argon2id$v=19$m=19456,t=2,p=1$&lt;salt&gt;$&lt;hash&gt;</c>) so parameters travel with the hash.
/// Current policy (OWASP balanced): m=19456 KiB, t=2, p=1. Verify reports a rehash when the stored
/// parameters are below current policy. Behind <see cref="IPasswordHasher"/> so the algorithm is swappable.
/// </summary>
public sealed class Argon2idPasswordHasher : IPasswordHasher
{
    private const int CurrentMemoryKiB = 19456;
    private const int CurrentIterations = 2;
    private const int CurrentParallelism = 1;
    private const int SaltLength = 16;
    private const int HashLength = 32;
    private const int Version = 19;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Compute(password, salt, CurrentMemoryKiB, CurrentIterations, CurrentParallelism);
        return Encode(CurrentMemoryKiB, CurrentIterations, CurrentParallelism, salt, hash);
    }

    public PasswordVerificationResult Verify(string hash, string password)
    {
        if (!TryDecode(hash, out var p))
            return new PasswordVerificationResult(false, false);

        var computed = Compute(password, p.Salt, p.MemoryKiB, p.Iterations, p.Parallelism);
        var ok = CryptographicOperations.FixedTimeEquals(computed, p.Hash);
        if (!ok)
            return new PasswordVerificationResult(false, false);

        var needsRehash =
            p.MemoryKiB < CurrentMemoryKiB ||
            p.Iterations < CurrentIterations ||
            p.Parallelism != CurrentParallelism;

        return new PasswordVerificationResult(true, needsRehash);
    }

    private static byte[] Compute(string password, byte[] salt, int memoryKiB, int iterations, int parallelism)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKiB,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };
        return argon2.GetBytes(HashLength);
    }

    private static string Encode(int memoryKiB, int iterations, int parallelism, byte[] salt, byte[] hash) =>
        $"$argon2id$v={Version}$m={memoryKiB},t={iterations},p={parallelism}$" +
        $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

    private static bool TryDecode(string phc, out DecodedHash decoded)
    {
        decoded = default;
        if (string.IsNullOrWhiteSpace(phc))
            return false;

        // Expected: ["", "argon2id", "v=19", "m=..,t=..,p=..", "<salt>", "<hash>"]
        var parts = phc.Split('$');
        if (parts.Length != 6 || parts[1] != "argon2id")
            return false;

        var paramMap = parts[3]
            .Split(',')
            .Select(kv => kv.Split('=', 2))
            .Where(kv => kv.Length == 2)
            .ToDictionary(kv => kv[0], kv => kv[1]);

        if (!TryParse(paramMap, "m", out var memoryKiB) ||
            !TryParse(paramMap, "t", out var iterations) ||
            !TryParse(paramMap, "p", out var parallelism))
            return false;

        try
        {
            decoded = new DecodedHash(
                memoryKiB,
                iterations,
                parallelism,
                Convert.FromBase64String(parts[4]),
                Convert.FromBase64String(parts[5]));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryParse(IReadOnlyDictionary<string, string> map, string key, out int value)
    {
        value = 0;
        return map.TryGetValue(key, out var raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private readonly record struct DecodedHash(int MemoryKiB, int Iterations, int Parallelism, byte[] Salt, byte[] Hash);
}
