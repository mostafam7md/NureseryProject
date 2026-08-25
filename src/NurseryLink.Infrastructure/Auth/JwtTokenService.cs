using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NurseryLink.Application.Common;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtSettings> options) : ITokenService
{
    private const int RefreshTokenBytes = 32;

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(options.Value.RefreshTokenExpiryInDays);

    public (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(
        Account account,
        IReadOnlyCollection<Privilege> effectivePrivileges)
    {
        var settings = options.Value;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.UserName),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.GivenName, account.FullName),
            new(ClaimTypes.Role, account.AccountType.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
        };

        claims.AddRange(effectivePrivileges
            .Distinct()
            .Select(p => new Claim(AppClaims.Privilege, p.ToString())));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(settings.AccessTokenExpiryInMinutes);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    /// <summary>
    /// Produces an opaque, cryptographically random token plus the hash to persist. The raw value
    /// is returned to the caller once and never stored, so a leaked database yields no usable
    /// sessions.
    /// </summary>
    public (string Token, string TokenHash) CreateRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        return (token, HashRefreshToken(token));
    }

    /// <summary>
    /// Plain SHA-256, not a password hash. The token is 256 bits of entropy rather than a guessable
    /// secret, so there is nothing for a slow KDF to defend against — and refresh has to be fast.
    /// </summary>
    public string HashRefreshToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
