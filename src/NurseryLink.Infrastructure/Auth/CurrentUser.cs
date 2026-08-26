using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NurseryLink.Application.Common;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Infrastructure.Auth;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public AccountId? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
            ? AccountId.From(parsed)
            : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin =>
        Principal?.HasClaim(ClaimTypes.Role, AppRoles.Admin) ?? false;

    public bool IsTeacher =>
        Principal?.HasClaim(ClaimTypes.Role, AppRoles.Teacher) ?? false;

    public bool IsParent =>
        Principal?.HasClaim(ClaimTypes.Role, AppRoles.Parent) ?? false;

    public bool HasPrivilege(Privilege privilege) =>
        Principal?.HasClaim(AppClaims.Privilege, privilege.ToString()) ?? false;

    public string? IpAddress =>
        accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
