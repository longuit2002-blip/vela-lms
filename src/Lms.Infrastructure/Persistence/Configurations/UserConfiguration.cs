using Lms.Domain.Organizations;
using Lms.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.OrganizationId).HasColumnName("organization_id").IsRequired();

        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        // Email is normalized lowercase in the domain; uniqueness is per-organization.
        builder.HasIndex(u => new { u.OrganizationId, u.Email }).IsUnique();

        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.MustChangePassword).HasColumnName("must_change_password").IsRequired();
        builder.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count").IsRequired();
        builder.Property(u => u.LockoutEndsAt).HasColumnName("lockout_ends_at");

        builder.PrimitiveCollection(u => u.RoleCodes)
            .HasColumnName("role_codes")
            .IsRequired();

        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");

        // FK to the tenant root (no navigation needed on the aggregate).
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
