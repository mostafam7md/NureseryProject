using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.FullName).HasMaxLength(NurseryConstants.FullNameMaxLength).IsRequired();
        builder.Property(s => s.StudentCode).HasMaxLength(NurseryConstants.StudentCodeMaxLength).IsRequired();
        builder.Property(s => s.DateOfBirth).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAtUtc).IsRequired();

        builder.HasIndex(s => s.StudentCode).IsUnique();
        builder.HasIndex(s => s.CurrentClassId);

        builder.HasOne(s => s.Class)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.CurrentClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
