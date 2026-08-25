using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Admins.Dtos;

public sealed record CreateAdminRequest(
    string FullName,
    string UserName,
    string Email,
    string Password,
    IReadOnlyCollection<Privilege> Privileges);

public sealed record AdminResponse(
    AccountId Id,
    string FullName,
    string UserName,
    string Email,
    bool IsActive,
    bool IsSuperAdmin,
    AccountId? CreatedByAdminId,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<string> Privileges);
