using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class ClassTeacherConfiguration : IEntityTypeConfiguration<ClassTeacher>
{
    public void Configure(EntityTypeBuilder<ClassTeacher> builder)
    {
        builder.ToTable("ClassTeachers");

        builder.HasKey(ct => new { ct.ClassId, ct.TeacherAccountId });

        builder.Property(ct => ct.AssignedAtUtc).IsRequired();

        builder.HasIndex(ct => ct.TeacherAccountId);

        builder.HasOne(ct => ct.Class)
            .WithMany(c => c.ClassTeachers)
            .HasForeignKey(ct => ct.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.TeacherAccount)
            .WithMany(t => t.ClassTeachers)
            .HasForeignKey(ct => ct.TeacherAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
