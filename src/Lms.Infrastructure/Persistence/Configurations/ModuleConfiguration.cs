using Lms.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lms.Infrastructure.Persistence.Configurations;

public sealed class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("modules");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(m => m.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(m => m.Order).HasColumnName("order_index");

        builder.HasIndex(m => m.CourseId);

        // Owned lesson collection (within the Course aggregate) via the private backing field.
        builder.HasMany(m => m.Lessons)
            .WithOne()
            .HasForeignKey(l => l.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(m => m.Lessons).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
