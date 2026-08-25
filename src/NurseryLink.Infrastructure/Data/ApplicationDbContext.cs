using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Domain.Entities;
using NurseryLink.Infrastructure.Data.Converters;

namespace NurseryLink.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    private const int SqlServerDuplicateKeyRow = 2601;
    private const int SqlServerUniqueConstraintViolation = 2627;

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<AdminPrivilege> AdminPrivileges => Set<AdminPrivilege>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<ClassTeacher> ClassTeachers => Set<ClassTeacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<ParentStudent> ParentStudents => Set<ParentStudent>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ParentNotification> ParentNotifications => Set<ParentNotification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // One registration per id type covers every property of that type across the model,
        // including the nullable form (Admin.CreatedByAdminId, RefreshToken.ReplacedByTokenId).
        configurationBuilder.Properties<AccountId>().HaveConversion<StronglyTypedIdValueConverter<AccountId>>();
        configurationBuilder.Properties<StudentId>().HaveConversion<StronglyTypedIdValueConverter<StudentId>>();
        configurationBuilder.Properties<ClassId>().HaveConversion<StronglyTypedIdValueConverter<ClassId>>();
        configurationBuilder.Properties<ClassTeacherId>().HaveConversion<StronglyTypedIdValueConverter<ClassTeacherId>>();
        configurationBuilder.Properties<ActivityLogId>().HaveConversion<StronglyTypedIdValueConverter<ActivityLogId>>();
        configurationBuilder.Properties<NotificationId>().HaveConversion<StronglyTypedIdValueConverter<NotificationId>>();
        configurationBuilder.Properties<AuditLogId>().HaveConversion<StronglyTypedIdValueConverter<AuditLogId>>();
        configurationBuilder.Properties<RefreshTokenId>().HaveConversion<StronglyTypedIdValueConverter<RefreshTokenId>>();

        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Translates unique-index violations into <see cref="ConflictException"/> so a lost race
    /// between a service's pre-check and the insert becomes a 409 instead of an unhandled 500.
    /// Keeping this here means the provider-specific error codes stay in Infrastructure.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new ConflictException(
                "That value is already taken by another record. Please use a different one.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sql &&
        sql.Errors.Cast<SqlError>().Any(e =>
            e.Number is SqlServerDuplicateKeyRow or SqlServerUniqueConstraintViolation);
}
