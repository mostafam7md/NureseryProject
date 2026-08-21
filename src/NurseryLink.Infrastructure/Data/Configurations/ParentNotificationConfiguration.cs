using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class ParentNotificationConfiguration : IEntityTypeConfiguration<ParentNotification>
{
    public void Configure(EntityTypeBuilder<ParentNotification> builder)
    {
        builder.ToTable("ParentNotifications");

        builder.HasKey(pn => new { pn.NotificationId, pn.ParentAccountId });

        builder.HasIndex(pn => pn.ParentAccountId);
        builder.HasIndex(pn => pn.IsRead);

        builder.HasOne(pn => pn.Notification)
            .WithMany(n => n.ParentNotifications)
            .HasForeignKey(pn => pn.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pn => pn.ParentAccount)
            .WithMany(p => p.Notifications)
            .HasForeignKey(pn => pn.ParentAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
