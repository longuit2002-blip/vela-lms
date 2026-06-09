using Lms.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.ModuleId).HasColumnName("module_id").IsRequired();
        builder.Property(l => l.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(l => l.Order).HasColumnName("order_index");
        builder.Property(l => l.Type).HasColumnName("type");
        builder.Property(l => l.VideoUrl).HasColumnName("video_url").HasMaxLength(2000).IsRequired();
        builder.Property(l => l.DurationSeconds).HasColumnName("duration_seconds");

        builder.HasIndex(l => l.ModuleId);
    }
}
