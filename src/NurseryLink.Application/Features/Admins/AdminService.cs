using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Extensions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Application.Common.Services;
using NurseryLink.Application.Features.Admins.Dtos;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Admins;

public interface IAdminService
{
    Task<AdminResponse> CreateAsync(CreateAdminRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AdminResponse> GetByIdAsync(AccountId adminId, CancellationToken cancellationToken = default);
}

public sealed class AdminService(
    IApplicationDbContext db,
    IAdminGuard adminGuard,
    IPasswordHasher passwordHasher,
    INurseryClock clock,
    IValidator<CreateAdminRequest> createValidator) : IAdminService
{
    public async Task<AdminResponse> CreateAsync(CreateAdminRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageAdmins, cancellationToken);
        var requestedPrivileges = request.Privileges.Distinct().ToArray();

        // ---- Authorization first, data checks second. -------------------------------------
        // Ordering matters: if a forbidden request also happened to use a taken username, running
        // the uniqueness check first would answer 409 and hide the 403.

        // No privilege escalation: an admin can only grant privileges it effectively holds.
        // Read from the database rather than the caller's token claims, so a privilege revoked
        // after the token was issued cannot still be handed out.
        var allowed = caller.EffectivePrivileges();
        var illegal = requestedPrivileges.Except(allowed).ToArray();
        if (illegal.Length > 0)
        {
            throw new ForbiddenException(
                $"You cannot grant privileges you do not hold: {string.Join(", ", illegal)}.");
        }

        // PLAN.md A13: ManageAdmins is reserved to the seeded super admin. This is stricter than
        // the written requirement (which only forbids granting privileges you do not hold) and is
        // a deliberate choice to keep the admin tree rooted at one account.
        if (requestedPrivileges.Contains(Privilege.ManageAdmins) && !caller.IsSeeded)
        {
            throw new ForbiddenException("Only the super admin can grant the ManageAdmins privilege.");
        }

        // ---- Data checks. -----------------------------------------------------------------

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var normalizedUserName = Account.Normalize(userName);
        var normalizedEmail = Account.Normalize(email);

        // Friendly pre-check. The unique indexes are the actual guarantee — see the note at
        // SaveChanges, which covers the window between this query and the insert.
        if (await db.Accounts.AnyAsync(
                a => a.NormalizedUserName == normalizedUserName || a.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            throw new ConflictException("An account with the same username or email already exists.");
        }

        var now = clock.UtcNow;
        var admin = new Admin
        {
            FullName = request.FullName.Trim(),
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            AccountType = AccountType.Admin,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByAdminId = caller.Id
        };

        db.Admins.Add(admin);
        db.AdminPrivileges.AddRange(requestedPrivileges.Select(p => new AdminPrivilege
        {
            AdminAccountId = admin.Id,
            Privilege = p
        }));

        db.AuditLogs.Add(new AuditLog
        {
            AdminAccountId = caller.Id,
            Action = AuditActions.AdminCreated,
            TargetType = nameof(Admin),
            TargetId = admin.Id.ToString(),
            Details = $"Created admin '{admin.UserName}' with privileges [{string.Join(", ", requestedPrivileges)}].",
            CreatedAtUtc = now
        });

        // Two concurrent requests can both pass the pre-check above; the unique index is what
        // actually stops the second one. ApplicationDbContext.SaveChangesAsync translates that
        // violation into a ConflictException, so it surfaces as 409 rather than an unhandled 500.
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(admin, requestedPrivileges);
    }

    public async Task<IReadOnlyList<AdminResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageAdmins, cancellationToken);

        var admins = await db.Admins
            .AsNoTracking()
            .Include(a => a.Privileges)
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return [.. admins.Select(a => ToResponse(a, a.EffectivePrivileges()))];
    }

    public async Task<AdminResponse> GetByIdAsync(AccountId adminId, CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageAdmins, cancellationToken);

        var admin = await db.Admins
            .AsNoTracking()
            .Include(a => a.Privileges)
            .SingleOrDefaultAsync(a => a.Id == adminId, cancellationToken)
            ?? throw new NotFoundException("Admin not found.");

        return ToResponse(admin, admin.EffectivePrivileges());
    }

    private static AdminResponse ToResponse(Admin admin, IEnumerable<Privilege> privileges) =>
        new(
            admin.Id,
            admin.FullName,
            admin.UserName,
            admin.Email,
            admin.IsActive,
            admin.IsSeeded,
            admin.CreatedByAdminId,
            admin.CreatedAtUtc,
            privileges.PrivilegeNames());
}
