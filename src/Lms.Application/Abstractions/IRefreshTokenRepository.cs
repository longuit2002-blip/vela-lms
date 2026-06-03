using Lms.Domain.Identity;

namespace Lms.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="RefreshToken"/> family. Implemented in Infrastructure over
/// EF Core. The consume operation is atomic at the database to make rotation race-safe.
/// </summary>
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically consumes an active token: a single guarded
    /// <c>UPDATE ... WHERE used_at IS NULL AND revoked_at IS NULL ... RETURNING</c> that marks it
    /// rotated (used + revoked 'rotated', <c>replaced_by = childId</c>). Returns the consumed token if
    /// this caller won the race, or <c>null</c> if it was already consumed/revoked (lost race or reuse).
    /// </summary>
    Task<RefreshToken?> TryConsumeAsync(string tokenHash, Guid childId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Revokes every still-active token in a family (reuse detection, password change, logout-all).</summary>
    Task RevokeFamilyAsync(Guid familyId, string reason, DateTimeOffset now, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
