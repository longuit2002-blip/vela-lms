using Lms.Domain.Departments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class DepartmentClosureConfiguration : IEntityTypeConfiguration<DepartmentClosureRow>
{
    public void Configure(EntityTypeBuilder<DepartmentClosureRow> builder)
    {
        builder.ToTable("department_closure");

        builder.HasKey(c => new { c.AncestorId, c.DescendantId });

        builder.Property(c => c.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(c => c.AncestorId).HasColumnName("ancestor_id");
        builder.Property(c => c.DescendantId).HasColumnName("descendant_id");
        builder.Property(c => c.Depth).HasColumnName("depth").IsRequired();

        builder.HasIndex(c => c.DescendantId);

        // Closure rows are maintained explicitly by the repository; FKs Restrict (the repository
        // removes a department's closure rows before removing the department).
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(c => c.AncestorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(c => c.DescendantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
