using System.ComponentModel.DataAnnotations;

namespace NurseryLink.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>Minimum key length for HMAC-SHA256, in bytes.</summary>
    public const int MinimumSecretKeyBytes = 32;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// HMAC signing key. Intentionally has no default: an app that boots with a placeholder key
    /// nobody replaced is worse than one that refuses to start. Supply it through user-secrets in
    /// development and an environment variable everywhere else.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Access-token lifetime. Deliberately short: privileges are carried in the token's claims, so
    /// this is also the worst-case delay before a deactivation or privilege change takes effect.
    /// </summary>
    [Range(1, 60)]
    public int AccessTokenExpiryInMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenExpiryInDays { get; init; } = 14;
}

public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    [Required(AllowEmptyStrings = false)]
    public string UserName { get; init; } = "superadmin";

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string Email { get; init; } = "superadmin@nurserylink.local";

    [Required(AllowEmptyStrings = false)]
    public string FullName { get; init; } = "System Super Admin";

    /// <summary>No default, for the same reason as <see cref="JwtSettings.SecretKey"/>.</summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(12)]
    public string Password { get; init; } = string.Empty;
}
