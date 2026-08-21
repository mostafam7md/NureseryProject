using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Infrastructure.Data.Configurations;

public class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("Admins");

        builder.HasIndex(a => a.CreatedByAdminId);

        builder.HasOne(a => a.CreatedByAdmin)
            .WithMany(a => a.CreatedAdmins)
            .HasForeignKey(a => a.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
