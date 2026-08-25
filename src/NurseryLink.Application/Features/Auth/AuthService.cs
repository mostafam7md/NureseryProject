using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Extensions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Application.Features.Auth.Dtos;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task<AccountResponse> GetMeAsync(CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ICurrentUser currentUser,
    INurseryClock clock,
    ILogger<AuthService> logger,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalized = Account.Normalize(request.UserNameOrEmail);

        var account = await db.Accounts
            .SingleOrDefaultAsync(
                a => a.NormalizedUserName == normalized || a.NormalizedEmail == normalized,
                cancellationToken);

        if (account is null || account.PasswordHash.Length == 0)
        {
            // Burn a comparable amount of CPU so a missing account is not detectably faster than a
            // wrong password. Without this the endpoint is a user-enumeration oracle.
            _ = passwordHasher.Hash(request.Password);
            throw new UnauthorizedException();
        }

        var outcome = passwordHasher.Verify(account.PasswordHash, request.Password);
        if (outcome == PasswordVerificationOutcome.Failed)
        {
            throw new UnauthorizedException();
        }

        if (!account.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated.");
        }

        if (outcome == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            account.PasswordHash = passwordHasher.Hash(request.Password);
        }

        var (response, _) = await CreateSessionAsync(account, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return response;
    }

    /// <summary>
    /// Exchanges a refresh token for a new access token, re-reading <c>IsActive</c> and privileges
    /// from the database. This is what makes deactivation and privilege changes take effect within
    /// one access-token lifetime instead of never.
    /// </summary>
    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        await refreshValidator.ValidateAndThrowAsync(request, cancellationToken);

        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var now = clock.UtcNow;

        var stored = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (stored.RevokedAtUtc is not null)
        {
            // Tokens are single-use. Seeing one twice means either a replay or a stolen token that
            // the legitimate client has already rotated past, so the whole family is burned.
            logger.LogWarning(
                "Refresh token reuse detected for account {AccountId}; revoking all active sessions.",
                stored.AccountId);

            await RevokeAllActiveTokensAsync(stored.AccountId, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (stored.IsExpiredAt(now))
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        var account = await db.Accounts
            .SingleOrDefaultAsync(a => a.Id == stored.AccountId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!account.IsActive)
        {
            await RevokeAllActiveTokensAsync(account.Id, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            throw new ForbiddenException("This account has been deactivated.");
        }

        var (response, issued) = await CreateSessionAsync(account, cancellationToken);

        stored.RevokedAtUtc = now;
        stored.ReplacedByTokenId = issued.Id;

        await db.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        await refreshValidator.ValidateAndThrowAsync(request, cancellationToken);

        var hash = tokenService.HashRefreshToken(request.RefreshToken);

        var stored = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        // Logging out an unknown or already-revoked token is a no-op rather than an error: the
        // caller's intent (this session should not work) is satisfied either way, and reporting the
        // difference would tell an attacker whether a token was real.
        if (stored is { RevokedAtUtc: null })
        {
            stored.RevokedAtUtc = clock.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AccountResponse> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var account = await db.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        if (!account.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated.");
        }

        var privileges = await GetPrivilegesAsync(account, cancellationToken);
        return ToResponse(account, privileges);
    }

    private async Task<(AuthResponse Response, RefreshToken Issued)> CreateSessionAsync(
        Account account,
        CancellationToken cancellationToken)
    {
        var privileges = await GetPrivilegesAsync(account, cancellationToken);
        var (accessToken, accessExpiresAtUtc) = tokenService.CreateAccessToken(account, privileges);
        var (refreshToken, refreshHash) = tokenService.CreateRefreshToken();

        var now = clock.UtcNow;
        var issued = new RefreshToken
        {
            AccountId = account.Id,
            TokenHash = refreshHash,
            ExpiresAtUtc = now.Add(tokenService.RefreshTokenLifetime),
            CreatedByIp = currentUser.IpAddress,
            CreatedAtUtc = now
        };

        db.RefreshTokens.Add(issued);

        var response = new AuthResponse(
            accessToken,
            accessExpiresAtUtc,
            refreshToken,
            issued.ExpiresAtUtc,
            ToResponse(account, privileges));

        return (response, issued);
    }

    private async Task RevokeAllActiveTokensAsync(AccountId accountId, DateTime now, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(t => t.AccountId == accountId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAtUtc = now;
        }
    }

    private async Task<List<Privilege>> GetPrivilegesAsync(Account account, CancellationToken cancellationToken)
    {
        if (account.AccountType != AccountType.Admin)
        {
            return [];
        }

        if (account.IsSeeded)
        {
            return [.. Enum.GetValues<Privilege>()];
        }

        return await db.AdminPrivileges
            .Where(p => p.AdminAccountId == account.Id)
            .Select(p => p.Privilege)
            .ToListAsync(cancellationToken);
    }

    private static AccountResponse ToResponse(Account account, IEnumerable<Privilege> privileges) =>
        new(
            account.Id,
            account.FullName,
            account.UserName,
            account.Email,
            account.AccountType.ToString(),
            account.IsActive,
            [.. privileges.Select(p => p.ToString()).Order()]);
}
