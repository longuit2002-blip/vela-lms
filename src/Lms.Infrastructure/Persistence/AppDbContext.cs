using Lms.Application.Abstractions;
using Lms.Domain.Departments;
using Lms.Domain.Identity;
using Lms.Domain.Organizations;
using Lms.Domain.Positions;
using Lms.Domain.Roles;
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
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentClosureRow> DepartmentClosure => Set<DepartmentClosureRow>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Tenant-owned entities are scoped to the current organization. `organizations` is the tenant
        // root and is intentionally NOT filtered (looked up by id from the JWT). Login/refresh bypass
        // via IgnoreQueryFilters (pre-tenant flows).
        modelBuilder.Entity<User>().HasQueryFilter(u => u.OrganizationId == _orgId);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(t => t.OrganizationId == _orgId);

        // New tenant-owned tables join the filter. `Role` is intentionally NOT filtered — system roles
        // are an org-wide catalog (organization_id NULL), like `organizations` (the tenant root).
        modelBuilder.Entity<Department>().HasQueryFilter(d => d.OrganizationId == _orgId);
        modelBuilder.Entity<DepartmentClosureRow>().HasQueryFilter(c => c.OrganizationId == _orgId);
        modelBuilder.Entity<Position>().HasQueryFilter(p => p.OrganizationId == _orgId);

        base.OnModelCreating(modelBuilder);
    }
}
