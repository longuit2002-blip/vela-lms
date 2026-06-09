using Lms.Application.Abstractions;
using Lms.Domain.Courses;
using Lms.Domain.Departments;
using Lms.Domain.Identity;
using Lms.Domain.Learning;
using Lms.Domain.Organizations;
using Lms.Domain.Positions;
using Lms.Domain.Publishing;
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
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

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

        // Learning-loop aggregate roots. Only the roots carry organization_id and are filtered here;
        // their child entities (modules, lessons, lesson_progress) have no tenant column and are
        // isolated transitively — they are only ever loaded through a tenant-scoped parent.
        modelBuilder.Entity<Course>().HasQueryFilter(c => c.OrganizationId == _orgId);
        modelBuilder.Entity<Publication>().HasQueryFilter(p => p.OrganizationId == _orgId);
        modelBuilder.Entity<Enrollment>().HasQueryFilter(e => e.OrganizationId == _orgId);

        base.OnModelCreating(modelBuilder);
    }
}
