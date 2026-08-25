using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;
using NurseryLink.Infrastructure.Configuration;
using NurseryLink.Infrastructure.Auth;
using NurseryLink.Infrastructure.Data;

namespace NurseryLink.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var settings = provider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var db = provider.GetRequiredService<ApplicationDbContext>();

        if (settings.MigrateOnStartup)
        {
            logger.LogInformation("Applying pending EF Core migrations.");
            await db.Database.MigrateAsync(cancellationToken);
        }
        else if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            // Deploy-time migration is the safer default, but silently running against an outdated
            // schema is worse than a loud warning.
            logger.LogWarning(
                "Database has pending migrations and {Section}:MigrateOnStartup is disabled. "
                + "Run 'dotnet ef database update' before serving traffic.",
                DatabaseSettings.SectionName);
        }

        if (settings.SeedOnStartup)
        {
            await SeedSuperAdminAsync(db, provider, logger, cancellationToken);
        }
    }

    private static async Task SeedSuperAdminAsync(
        ApplicationDbContext db,
        IServiceProvider provider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Accounts.AnyAsync(a => a.IsSeeded, cancellationToken))
        {
            return;
        }

        var options = provider.GetRequiredService<IOptions<SeedAdminOptions>>().Value;

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            // Hashing an empty string would create a real account nobody can sign in to and
            // nobody notices. Fail loudly instead.
            throw new InvalidOperationException(
                $"{SeedAdminOptions.SectionName}:Password is not configured. Set it via user-secrets "
                + "or an environment variable before first startup.");
        }

        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

        var superAdmin = new Admin
        {
            FullName = options.FullName,
            UserName = options.UserName,
            NormalizedUserName = Account.Normalize(options.UserName),
            Email = options.Email,
            NormalizedEmail = Account.Normalize(options.Email),
            PasswordHash = passwordHasher.Hash(options.Password),
            AccountType = AccountType.Admin,
            IsActive = true,
            IsSeeded = true
        };

        db.Admins.Add(superAdmin);

        // Rows are written for completeness/auditing, but the seeded admin's effective privileges
        // are computed as "all of them" regardless, so a privilege added to the enum later is
        // picked up without a data migration.
        db.AdminPrivileges.AddRange(Enum.GetValues<Privilege>().Select(p => new AdminPrivilege
        {
            AdminAccountId = superAdmin.Id,
            Privilege = p
        }));

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded super admin account '{UserName}' with all privileges.", superAdmin.UserName);
    }
}
