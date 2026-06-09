using Lms.Domain.Learning;
using Lms.Domain.Organizations;
using Lms.Domain.Publishing;
using Lms.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.PublicationId).HasColumnName("publication_id").IsRequired();
        builder.Property(e => e.Source).HasColumnName("source");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.ProgressPercent).HasColumnName("progress_percent");
        builder.Property(e => e.StartedAt).HasColumnName("started_at");
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.PublicationId }).IsUnique();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Publication>()
            .WithMany()
            .HasForeignKey(e => e.PublicationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Owned lesson-progress collection (within the Enrollment aggregate) via the backing field.
        builder.HasMany(e => e.LessonProgress)
            .WithOne()
            .HasForeignKey(p => p.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.LessonProgress).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
