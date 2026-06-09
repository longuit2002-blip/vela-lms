using Lms.Domain.Organizations;
using Lms.Domain.Publishing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("publications");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(p => p.Kind).HasColumnName("kind").HasMaxLength(50).IsRequired();
        builder.Property(p => p.ContentId).HasColumnName("content_id").IsRequired();
        builder.Property(p => p.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(p => p.Status).HasColumnName("status");
        builder.Property(p => p.PublishedAt).HasColumnName("published_at");
        builder.Property(p => p.PublishedBy).HasColumnName("published_by");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => new { p.OrganizationId, p.ContentId });

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
