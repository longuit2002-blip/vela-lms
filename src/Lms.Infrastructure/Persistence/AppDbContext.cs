using Lms.Application.Abstractions;
using Lms.Domain.Identity;
using Lms.Domain.Organizations;
using Lms.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : DbContext(options)
{
    // Captured as a scalar at construction (the EF-recommended pattern) — never a service call inside
    // the filter lambda. Empty when no tenant → tenant-owned queries match nothing (fail closed).
    private readonly Guid _orgId = tenant.OrganizationId;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Tenant-owned entities are scoped to the current organization. `organizations` is the tenant
        // root and is intentionally NOT filtered (looked up by id from the JWT). Login/refresh bypass
        // via IgnoreQueryFilters (pre-tenant flows).
        modelBuilder.Entity<User>().HasQueryFilter(u => u.OrganizationId == _orgId);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(t => t.OrganizationId == _orgId);

        base.OnModelCreating(modelBuilder);
    }
}
