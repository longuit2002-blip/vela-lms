using Lms.Domain.Organizations;
using Lms.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        // Null for system roles (org-wide catalog); set for future org-defined custom roles.
        builder.Property(r => r.OrganizationId).HasColumnName("organization_id");
        builder.Property(r => r.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(r => r.IsSystem).HasColumnName("is_system").IsRequired();

        builder.PrimitiveCollection(r => r.Permissions).HasColumnName("permissions").IsRequired();

        // System role codes are globally unique (organization_id IS NULL). A future slice that adds
        // org-defined roles would add a separate (organization_id, code) unique index for those.
        builder.HasIndex(r => r.Code).IsUnique().HasFilter("organization_id IS NULL");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
