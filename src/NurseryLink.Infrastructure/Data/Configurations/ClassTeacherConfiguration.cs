using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class ClassTeacherConfiguration : IEntityTypeConfiguration<ClassTeacher>
{
    public void Configure(EntityTypeBuilder<ClassTeacher> builder)
    {
        builder.ToTable("ClassTeachers", table =>
        {
            table.HasCheckConstraint(
                "CK_ClassTeachers_EndsAfterStart",
                "[EndedAtUtc] IS NULL OR [EndedAtUtc] > [StartedAtUtc]");
        });

        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.AssignmentType).HasConversion<int>().IsRequired();
        builder.Property(ct => ct.StartedAtUtc).IsRequired();
        builder.Property(ct => ct.CreatedAtUtc).IsRequired();

        // "A class is assigned to exactly one teacher at a time." A filtered unique index makes
        // that a database guarantee rather than a service-layer convention, while still allowing
        // any number of closed (historical) rows and overlapping substitutes.
        builder.HasIndex(ct => ct.ClassId)
            .HasDatabaseName("IX_ClassTeachers_OneCurrentPermanentTeacherPerClass")
            .IsUnique()
            .HasFilter("[EndedAtUtc] IS NULL AND [AssignmentType] = 0");

        builder.HasIndex(ct => new { ct.ClassId, ct.EndedAtUtc });
        builder.HasIndex(ct => new { ct.TeacherAccountId, ct.EndedAtUtc });

        builder.HasOne(ct => ct.Class)
            .WithMany(c => c.ClassTeachers)
            .HasForeignKey(ct => ct.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, not Cascade: accounts are deactivated rather than deleted, and assignment
        // history must survive. A stray delete should fail loudly instead of erasing the record.
        builder.HasOne(ct => ct.TeacherAccount)
            .WithMany(t => t.ClassTeachers)
            .HasForeignKey(ct => ct.TeacherAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
