using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.UseTptMappingStrategy();

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName).HasMaxLength(NurseryConstants.FullNameMaxLength).IsRequired();
        builder.Property(a => a.UserName).HasMaxLength(NurseryConstants.UserNameMaxLength).IsRequired();
        builder.Property(a => a.Email).HasMaxLength(NurseryConstants.EmailMaxLength).IsRequired();
        builder.Property(a => a.PasswordHash).HasMaxLength(NurseryConstants.PasswordHashMaxLength).IsRequired();
        builder.Property(a => a.AccountType).HasConversion<int>().IsRequired();
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasIndex(a => a.UserName).IsUnique();
        builder.HasIndex(a => a.Email).IsUnique();
        builder.HasIndex(a => a.AccountType);
    }
}
