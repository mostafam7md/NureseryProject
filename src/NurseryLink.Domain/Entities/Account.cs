using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

public abstract class Account : BaseEntity<AccountId>
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Username as the admin typed it. Shown in the UI; never used for lookup.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Upper-invariant form of <see cref="UserName"/>. Carries the unique index and is the
    /// only column login matches on, so behaviour does not depend on database collation.</summary>
    public string NormalizedUserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Upper-invariant form of <see cref="Email"/>. See <see cref="NormalizedUserName"/>.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public AccountType AccountType { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>True only for the single default admin created at first startup. That account
    /// implicitly holds every privilege and may not be deactivated.</summary>
    public bool IsSeeded { get; set; }

    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
