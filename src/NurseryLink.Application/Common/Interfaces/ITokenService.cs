using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>Short-lived bearer token. Privileges are baked into the claims, which is why the
    /// lifetime is measured in minutes — see <see cref="RefreshTokenLifetime"/>.</summary>
    (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(
        Account account,
        IReadOnlyCollection<Privilege> effectivePrivileges);

    /// <summary>Generates a refresh token. The raw value goes to the client exactly once; only the
    /// hash is ever persisted.</summary>
    (string Token, string TokenHash) CreateRefreshToken();

    string HashRefreshToken(string token);

    TimeSpan RefreshTokenLifetime { get; }
}
