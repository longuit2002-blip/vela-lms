using Lms.Domain.Identity;
using Lms.Domain.Organizations;
using Lms.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.FamilyId).HasColumnName("family_id").IsRequired();
        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(t => t.OrganizationId).HasColumnName("organization_id").IsRequired();

        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.ParentId).HasColumnName("parent_id");
        builder.Property(t => t.ReplacedById).HasColumnName("replaced_by_id");

        builder.Property(t => t.IssuedAt).HasColumnName("issued_at");
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
        builder.Property(t => t.UsedAt).HasColumnName("used_at");
        builder.Property(t => t.RevokedAt).HasColumnName("revoked_at");
        builder.Property(t => t.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(40);

        builder.HasIndex(t => t.FamilyId);
        builder.HasIndex(t => new { t.UserId, t.OrganizationId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
