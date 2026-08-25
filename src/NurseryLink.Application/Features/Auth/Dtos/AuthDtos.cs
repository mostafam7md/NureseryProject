namespace NurseryLink.Application.Features.Auth.Dtos;

public sealed record LoginRequest(string UserNameOrEmail, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AccountResponse(
    AccountId Id,
    string FullName,
    string UserName,
    string Email,
    string Role,
    bool IsActive,
    IReadOnlyCollection<string> Privileges);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    AccountResponse Account);
