namespace NurseryLink.Infrastructure.Configuration;

public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    /// <summary>
    /// Whether to run EF migrations during startup. Convenient in development; discouraged in
    /// production, where concurrent instances race each other and the app would need DDL rights
    /// at runtime. Prefer 'dotnet ef database update' as a deploy step.
    /// </summary>
    public bool MigrateOnStartup { get; init; }

    /// <summary>Whether to create the default admin if no seeded account exists yet.</summary>
    public bool SeedOnStartup { get; init; } = true;
}
