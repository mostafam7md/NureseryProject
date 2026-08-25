using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .HasMaxLength(NurseryConstants.TokenHashLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(t => t.ExpiresAtUtc).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(NurseryConstants.IpAddressMaxLength);

        // Refresh lookups are by hash and must be a single seek.
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // Supports "revoke every live session for this account" on deactivation and reuse detection.
        builder.HasIndex(t => new { t.AccountId, t.RevokedAtUtc });

        builder.HasOne(t => t.Account)
            .WithMany(a => a.RefreshTokens)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
