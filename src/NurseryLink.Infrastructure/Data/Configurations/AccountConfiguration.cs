using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", table =>
        {
            // "There is one default Admin account ... This account cannot be deactivated."
            // Enforced in the schema so no future service method can violate it by omission.
            table.HasCheckConstraint(
                "CK_Accounts_SeededAccountStaysActive",
                "[IsSeeded] = 0 OR [IsActive] = 1");
        });

        builder.UseTptMappingStrategy();

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName).HasMaxLength(NurseryConstants.FullNameMaxLength).IsRequired();
        builder.Property(a => a.UserName).HasMaxLength(NurseryConstants.UserNameMaxLength).IsRequired();
        builder.Property(a => a.NormalizedUserName).HasMaxLength(NurseryConstants.UserNameMaxLength).IsRequired();
        builder.Property(a => a.Email).HasMaxLength(NurseryConstants.EmailMaxLength).IsRequired();
        builder.Property(a => a.NormalizedEmail).HasMaxLength(NurseryConstants.EmailMaxLength).IsRequired();
        builder.Property(a => a.PasswordHash).HasMaxLength(NurseryConstants.PasswordHashMaxLength).IsRequired();
        builder.Property(a => a.AccountType).HasConversion<int>().IsRequired();
        builder.Property(a => a.IsActive).IsRequired();
        builder.Property(a => a.IsSeeded).IsRequired();
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        // Uniqueness lives on the normalized columns, which is also what login matches on, so the
        // behaviour no longer depends on the database's collation being case-insensitive.
        builder.HasIndex(a => a.NormalizedUserName).IsUnique();
        builder.HasIndex(a => a.NormalizedEmail).IsUnique();
        builder.HasIndex(a => a.AccountType);

        builder.HasIndex(a => a.IsSeeded)
            .HasDatabaseName("IX_Accounts_OnlyOneSeededAdmin")
            .IsUnique()
            .HasFilter("[IsSeeded] = 1");
    }
}
