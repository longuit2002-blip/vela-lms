using Lms.Domain.Courses;
using Lms.Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(300).IsRequired();
        builder.Property(c => c.CategoryId).HasColumnName("category_id");
        builder.Property(c => c.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(1000);
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.Status).HasColumnName("status");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(c => c.OrganizationId);
        builder.HasIndex(c => new { c.OrganizationId, c.Slug }).IsUnique();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Owned child collection (within the aggregate) mapped to its own table via the private
        // backing field. Cascade: modules are part of the course and go with it.
        builder.HasMany(c => c.Modules)
            .WithOne()
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Modules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
