using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class AdminPrivilegeConfiguration : IEntityTypeConfiguration<AdminPrivilege>
{
    public void Configure(EntityTypeBuilder<AdminPrivilege> builder)
    {
        builder.ToTable("AdminPrivileges");

        builder.HasKey(ap => new { ap.AdminAccountId, ap.Privilege });

        builder.Property(ap => ap.Privilege).HasConversion<int>();

        builder.HasOne(ap => ap.AdminAccount)
            .WithMany(a => a.Privileges)
            .HasForeignKey(ap => ap.AdminAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
