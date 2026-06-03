using Lms.Application.Abstractions;
using Lms.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task AddAsync(User user, CancellationToken cancellationToken)
        => await db.Users.AddAsync(user, cancellationToken);

    public Task<User?> FindByEmailForLoginAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        // Login runs before tenant context exists — bypass the tenant query filter (RLS handled by
        // the maintenance connection, U5). Email is globally unique enough for login by (email) here;
        // a future multi-org-same-email model would disambiguate by org at the login screen.
        return db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(Guid organizationId, string email, CancellationToken cancellationToken)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        return db.Users.AnyAsync(u => u.OrganizationId == organizationId && u.Email == normalized, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
