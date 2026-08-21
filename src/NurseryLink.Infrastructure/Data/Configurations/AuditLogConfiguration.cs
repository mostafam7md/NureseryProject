using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasMaxLength(NurseryConstants.ActionMaxLength).IsRequired();
        builder.Property(a => a.TargetType).HasMaxLength(NurseryConstants.TargetTypeMaxLength).IsRequired();
        builder.Property(a => a.TargetId).HasMaxLength(NurseryConstants.TargetIdMaxLength).IsRequired();
        builder.Property(a => a.Details).HasMaxLength(NurseryConstants.DetailsMaxLength);
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasIndex(a => a.AdminAccountId);
        builder.HasIndex(a => new { a.TargetType, a.TargetId });

        builder.HasOne(a => a.AdminAccount)
            .WithMany(a => a.AuditLogs)
            .HasForeignKey(a => a.AdminAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
