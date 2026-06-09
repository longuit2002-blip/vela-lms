using Lms.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ToTable("lesson_progress");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.EnrollmentId).HasColumnName("enrollment_id").IsRequired();
        // lesson_id is a loose cross-aggregate reference (no FK) — the Course aggregate owns lessons.
        builder.Property(p => p.LessonId).HasColumnName("lesson_id").IsRequired();
        builder.Property(p => p.Status).HasColumnName("status");
        builder.Property(p => p.CompletedAt).HasColumnName("completed_at");

        builder.HasIndex(p => new { p.EnrollmentId, p.LessonId }).IsUnique();
    }
}
