using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Extensions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Common.Services;

/// <summary>
/// Resolves the calling admin from the database and re-checks their privileges there.
/// </summary>
/// <remarks>
/// The <c>[HasPrivilege]</c> attribute on the controller already rejects a caller whose *token*
/// lacks the privilege, so this looks redundant. It is not: the token is a snapshot taken at login,
/// and an admin who was deactivated or stripped of a privilege afterwards would otherwise keep
/// acting on it until the access token expired. Reading from the database closes that window.
/// </remarks>
public interface IAdminGuard
{
    /// <summary>Requires the caller to be an active admin account.</summary>
    Task<Admin> RequireAdminAsync(CancellationToken cancellationToken = default);

    /// <summary>Requires the caller to be an active admin account that currently holds
    /// <paramref name="privilege"/>.</summary>
    Task<Admin> RequireAdminAsync(Privilege privilege, CancellationToken cancellationToken = default);
}

public sealed class AdminGuard(IApplicationDbContext db, ICurrentUser currentUser) : IAdminGuard
{
    public async Task<Admin> RequireAdminAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        if (!currentUser.IsAdmin)
        {
            throw new ForbiddenException("Only admins may perform this action.");
        }

        var admin = await db.Admins
            .Include(a => a.Privileges)
            .SingleOrDefaultAsync(a => a.Id == userId, cancellationToken)
            ?? throw new ForbiddenException("Caller is not an admin account.");

        if (!admin.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated.");
        }

        return admin;
    }

    public async Task<Admin> RequireAdminAsync(Privilege privilege, CancellationToken cancellationToken = default)
    {
        var admin = await RequireAdminAsync(cancellationToken);

        if (!admin.EffectivePrivileges().Contains(privilege))
        {
            throw new ForbiddenException($"This action requires the {privilege} privilege.");
        }

        return admin;
    }
}
