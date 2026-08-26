using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Extensions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Application.Common.Services;
using NurseryLink.Application.Features.Teachers.Dtos;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Teachers;

public interface ITeacherService
{
    Task<TeacherResponse> CreateAsync(CreateTeacherRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<TeacherResponse> GetByIdAsync(AccountId teacherId, CancellationToken cancellationToken = default);

    Task<TeacherResponse> DeactivateAsync(AccountId teacherId, CancellationToken cancellationToken = default);

    Task<TeacherResponse> ReactivateAsync(AccountId teacherId, CancellationToken cancellationToken = default);
}

public sealed class TeacherService(
    IApplicationDbContext db,
    IAdminGuard adminGuard,
    IPasswordHasher passwordHasher,
    INurseryClock clock,
    IValidator<CreateTeacherRequest> createValidator) : ITeacherService
{
    public async Task<TeacherResponse> CreateAsync(
        CreateTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageTeachers, cancellationToken);

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var normalizedUserName = Account.Normalize(userName);
        var normalizedEmail = Account.Normalize(email);

        // Friendly pre-check across *all* account types, not just teachers: usernames and emails
        // are unique across the whole Accounts table because login matches on them globally.
        if (await db.Accounts.AnyAsync(
                a => a.NormalizedUserName == normalizedUserName || a.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            throw new ConflictException("An account with the same username or email already exists.");
        }

        var now = clock.UtcNow;
        var teacher = new Teacher
        {
            FullName = request.FullName.Trim(),
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            AccountType = AccountType.Teacher,
            IsActive = true,
            CreatedAtUtc = now
        };

        db.Teachers.Add(teacher);
        AddAudit(caller.Id, AuditActions.TeacherCreated, teacher.Id,
            $"Created teacher '{teacher.UserName}'.", now);

        // The unique indexes, not the pre-check above, are what actually settle a race between two
        // concurrent creates; ApplicationDbContext turns the violation into a 409.
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(teacher, []);
    }

    public async Task<IReadOnlyList<TeacherResponse>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageTeachers, cancellationToken);

        var teachers = await CurrentAssignmentsQuery(db.Teachers.AsNoTracking())
            .Where(t => includeInactive || t.IsActive)
            .OrderBy(t => t.FullName)
            .ToListAsync(cancellationToken);

        return [.. teachers.Select(t => ToResponse(t, t.ClassTeachers))];
    }

    public async Task<TeacherResponse> GetByIdAsync(
        AccountId teacherId,
        CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageTeachers, cancellationToken);

        var teacher = await CurrentAssignmentsQuery(db.Teachers.AsNoTracking())
            .SingleOrDefaultAsync(t => t.Id == teacherId, cancellationToken)
            ?? throw new NotFoundException("Teacher not found.");

        return ToResponse(teacher, teacher.ClassTeachers);
    }

    /// <summary>
    /// Deactivates a teacher who has left the nursery. Everything they logged stays in place and
    /// unchanged — the account row is never deleted, only flagged.
    /// </summary>
    public async Task<TeacherResponse> DeactivateAsync(
        AccountId teacherId,
        CancellationToken cancellationToken = default)
    {
        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageTeachers, cancellationToken);

        var teacher = await CurrentAssignmentsQuery(db.Teachers)
            .SingleOrDefaultAsync(t => t.Id == teacherId, cancellationToken)
            ?? throw new NotFoundException("Teacher not found.");

        if (!teacher.IsActive)
        {
            // Already in the requested state. Returning it rather than throwing keeps the call
            // idempotent and avoids an audit entry for a change that did not happen.
            return ToResponse(teacher, teacher.ClassTeachers);
        }

        // PLAN.md A9: a class must never be left without a teacher, so the reassignment has to
        // happen first. Refusing here is what forces that order.
        if (teacher.ClassTeachers.Count > 0)
        {
            var classNames = string.Join(", ", teacher.ClassTeachers
                .Select(a => a.Class?.Name ?? a.ClassId.ToString())
                .Order());

            throw new ConflictException(
                $"This teacher still holds {classNames}. Reassign the class to another teacher "
                + "before deactivating the account.");
        }

        var now = clock.UtcNow;
        teacher.IsActive = false;

        // Login already refuses inactive accounts and refresh re-reads IsActive, so this is not
        // what stops them signing in. It matters if the account is ever reactivated: without it,
        // a refresh token issued before the deactivation would start working again.
        await RevokeActiveTokensAsync(teacher.Id, now, cancellationToken);

        AddAudit(caller.Id, AuditActions.AccountDeactivated, teacher.Id,
            $"Deactivated teacher '{teacher.UserName}'.", now);

        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(teacher, teacher.ClassTeachers);
    }

    public async Task<TeacherResponse> ReactivateAsync(
        AccountId teacherId,
        CancellationToken cancellationToken = default)
    {
        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageTeachers, cancellationToken);

        var teacher = await CurrentAssignmentsQuery(db.Teachers)
            .SingleOrDefaultAsync(t => t.Id == teacherId, cancellationToken)
            ?? throw new NotFoundException("Teacher not found.");

        if (teacher.IsActive)
        {
            return ToResponse(teacher, teacher.ClassTeachers);
        }

        var now = clock.UtcNow;
        teacher.IsActive = true;

        AddAudit(caller.Id, AuditActions.AccountReactivated, teacher.Id,
            $"Reactivated teacher '{teacher.UserName}'.", now);

        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(teacher, teacher.ClassTeachers);
    }

    /// <summary>Loads only the tenures that are still open, so a teacher with years of assignment
    /// history does not drag all of it into memory on every read.</summary>
    private static IQueryable<Teacher> CurrentAssignmentsQuery(IQueryable<Teacher> source) =>
        source
            .Include(t => t.ClassTeachers.Where(a => a.EndedAtUtc == null))
            .ThenInclude(a => a.Class);

    private async Task RevokeActiveTokensAsync(AccountId accountId, DateTime now, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(t => t.AccountId == accountId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAtUtc = now;
        }
    }

    private void AddAudit(AccountId adminId, string action, AccountId targetId, string details, DateTime now) =>
        db.AuditLogs.Add(new AuditLog
        {
            AdminAccountId = adminId,
            Action = action,
            TargetType = nameof(Teacher),
            TargetId = targetId.ToString(),
            Details = details,
            CreatedAtUtc = now
        });

    private static TeacherResponse ToResponse(Teacher teacher, IEnumerable<ClassTeacher> currentAssignments) =>
        new(
            teacher.Id,
            teacher.FullName,
            teacher.UserName,
            teacher.Email,
            teacher.IsActive,
            teacher.CreatedAtUtc,
            [.. currentAssignments
                .OrderBy(a => a.StartedAtUtc)
                .Select(a => new TeacherAssignmentResponse(
                    a.Id,
                    a.ClassId,
                    a.Class?.Name ?? string.Empty,
                    a.AssignmentType.ToString(),
                    a.StartedAtUtc))]);
}
