using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).HasConversion<int>().IsRequired();
        builder.Property(n => n.Message).HasMaxLength(NurseryConstants.MessageMaxLength).IsRequired();
        builder.Property(n => n.CreatedAtUtc).IsRequired();

        builder.HasIndex(n => n.StudentId);
        builder.HasIndex(n => n.Type);

        builder.HasOne(n => n.Student)
            .WithMany(s => s.Notifications)
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
