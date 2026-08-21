using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class ParentStudentConfiguration : IEntityTypeConfiguration<ParentStudent>
{
    public void Configure(EntityTypeBuilder<ParentStudent> builder)
    {
        builder.ToTable("ParentStudents");

        builder.HasKey(ps => new { ps.ParentAccountId, ps.StudentId });

        builder.Property(ps => ps.CreatedAtUtc).IsRequired();

        builder.HasOne(ps => ps.ParentAccount)
            .WithMany(a => a.ParentStudents)
            .HasForeignKey(ps => ps.ParentAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Student)
            .WithMany(s => s.ParentStudents)
            .HasForeignKey(ps => ps.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
