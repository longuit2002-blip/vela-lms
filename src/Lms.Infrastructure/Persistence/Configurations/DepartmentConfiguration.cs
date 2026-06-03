using Lms.Domain.Departments;
using Lms.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(d => d.ParentId).HasColumnName("parent_id");
        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(d => d.OrganizationId);
        builder.HasIndex(d => new { d.OrganizationId, d.ParentId });

        // FK to the tenant root (no navigation).
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing parent FK. Restrict (not cascade): deletes are blocked by the handler when
        // the department has children or assigned users, so a cascade should never be the mechanism.
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
