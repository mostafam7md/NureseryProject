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
    DbSet<Class> Classes { get; }
    DbSet<ClassTeacher> ClassTeachers { get; }
    DbSet<Student> Students { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an explicit transaction so a change that needs more than one round trip stays atomic.
    /// Disposing without committing rolls back.
    /// </summary>
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
