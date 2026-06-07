using Lms.Application.Abstractions;
using Lms.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

/// <summary>
/// Role catalog persistence. The <c>roles</c> table is not tenant-filtered (system roles are an
/// org-wide catalog, <c>organization_id</c> null), so these queries see the full system catalog.
/// </summary>
public sealed class RoleRepository(AppDbContext db) : IRoleRepository
{
    public async Task AddAsync(Role role, CancellationToken cancellationToken)
        => await db.Roles.AddAsync(role, cancellationToken);

    public Task<bool> SystemRoleExistsAsync(string code, CancellationToken cancellationToken)
        => db.Roles.AnyAsync(r => r.OrganizationId == null && r.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken)
        => await db.Roles.Where(r => r.OrganizationId == null).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
