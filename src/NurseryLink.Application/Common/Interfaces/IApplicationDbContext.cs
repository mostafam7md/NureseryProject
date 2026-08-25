using Microsoft.EntityFrameworkCore;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Admin> Admins { get; }
    DbSet<Teacher> Teachers { get; }
    DbSet<Parent> Parents { get; }
    DbSet<AdminPrivilege> AdminPrivileges { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
