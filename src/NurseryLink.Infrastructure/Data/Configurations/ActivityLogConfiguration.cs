using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.LogType).HasConversion<int>().IsRequired();
        builder.Property(al => al.Payload)
            .HasMaxLength(NurseryConstants.ActivityPayloadMaxLength)
            .IsRequired();
        builder.Property(al => al.LoggedAtUtc).IsRequired();
        builder.Property(al => al.LocalDate).IsRequired();
        builder.Property(al => al.CreatedAtUtc).IsRequired();

        // Covers the two hot reads: a child's day (parent view, end-of-day summary) and the
        // one-meal-per-type-per-day check the service performs before inserting a meal.
        builder.HasIndex(al => new { al.StudentId, al.LocalDate, al.LogType });
        builder.HasIndex(al => new { al.ClassId, al.LocalDate });
        builder.HasIndex(al => al.LoggedByAccountId);

        builder.HasOne(al => al.Student)
            .WithMany(s => s.ActivityLogs)
            .HasForeignKey(al => al.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(al => al.Class)
            .WithMany(c => c.ActivityLogs)
            .HasForeignKey(al => al.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(al => al.LoggedByAccount)
            .WithMany(a => a.ActivityLogs)
            .HasForeignKey(al => al.LoggedByAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
