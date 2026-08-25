namespace NurseryLink.Domain.Entities;

/// <summary>
/// A single-use refresh token. Only the SHA-256 hash is stored, so a database leak does not hand
/// out working sessions. Refreshing revokes the presented token and issues a new one
/// (<see cref="ReplacedByTokenId"/> links the chain); replaying an already-revoked token is treated
/// as theft and revokes the whole chain.
/// </summary>
public class RefreshToken : BaseEntity<RefreshTokenId>
{
    public AccountId AccountId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public RefreshTokenId? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }

    public Account? Account { get; set; }

    public bool IsExpiredAt(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public bool IsActiveAt(DateTime utcNow) => RevokedAtUtc is null && !IsExpiredAt(utcNow);
}
