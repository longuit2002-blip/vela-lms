using Lms.Application.Abstractions;
using Lms.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
        => await db.RefreshTokens.AddAsync(token, cancellationToken);

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken)
        => db.RefreshTokens.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<RefreshToken?> TryConsumeAsync(string tokenHash, Guid childId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Atomic guarded consume. A data-modifying CTE lets EF map RETURNING * back to the entity in
        // one round trip: only the caller whose UPDATE matches the active-row predicate gets a row
        // back; concurrent/duplicate presentations get zero rows (caller treats null as lost/reuse).
        var rows = await db.RefreshTokens
            .FromSqlInterpolated($@"
                WITH consumed AS (
                    UPDATE refresh_tokens
                       SET used_at = {now}, revoked_at = {now}, revoked_reason = 'rotated', replaced_by_id = {childId}
                     WHERE token_hash = {tokenHash} AND used_at IS NULL AND revoked_at IS NULL
                    RETURNING *
                )
                SELECT * FROM consumed")
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    public Task RevokeFamilyAsync(Guid familyId, string reason, DateTimeOffset now, CancellationToken cancellationToken)
        => db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, now)
                    .SetProperty(t => t.RevokedReason, reason),
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
