using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Extensions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Application.Common.Services;
using NurseryLink.Application.Features.Parents.Dtos;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Parents;

public interface IParentService
{
    Task<ParentResponse> CreateAsync(CreateParentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParentResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<ParentResponse> GetByIdAsync(AccountId parentId, CancellationToken cancellationToken = default);

    Task<ParentResponse> LinkStudentAsync(AccountId parentId, StudentId studentId, CancellationToken cancellationToken = default);

    Task<ParentResponse> UnlinkStudentAsync(AccountId parentId, StudentId studentId, CancellationToken cancellationToken = default);

    Task<ParentResponse> DeactivateAsync(AccountId parentId, CancellationToken cancellationToken = default);

    Task<ParentResponse> ReactivateAsync(AccountId parentId, CancellationToken cancellationToken = default);
}

public sealed class ParentService(
    IApplicationDbContext db,
    IAdminGuard adminGuard,
    IPasswordHasher passwordHasher,
    INurseryClock clock,
    IValidator<CreateParentRequest> createValidator) : IParentService
{
    public async Task<ParentResponse> CreateAsync(
        CreateParentRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageParents, cancellationToken);

        var studentIds = request.StudentIds.Distinct().ToArray();
        var students = await RequireStudentsAsync(studentIds, cancellationToken);

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var normalizedUserName = Account.Normalize(userName);
        var normalizedEmail = Account.Normalize(email);

        if (await db.Accounts.AnyAsync(
                a => a.NormalizedUserName == normalizedUserName || a.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            throw new ConflictException("An account with the same username or email already exists.");
        }

        var now = clock.UtcNow;
        var parent = new Parent
        {
            FullName = request.FullName.Trim(),
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            AccountType = AccountType.Parent,
            IsActive = true,
            CreatedAtUtc = now
        };

        db.Parents.Add(parent);
        db.ParentStudents.AddRange(studentIds.Select(id => new ParentStudent
        {
            ParentAccountId = parent.Id,
            StudentId = id,
            CreatedAtUtc = now
        }));

        AddAudit(caller.Id, AuditActions.ParentCreated, parent.Id,
            $"Created parent '{parent.UserName}' linked to "
            + $"[{string.Join(", ", students.Select(s => s.StudentCode).Order())}].", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(parent.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ParentResponse>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageParents, cancellationToken);

        var parents = await WithDetails(db.Parents.AsNoTracking())
            .Where(p => includeInactive || p.IsActive)
            .OrderBy(p => p.FullName)
            .ToListAsync(cancellationToken);

        return [.. parents.Select(ToResponse)];
    }

    public async Task<ParentResponse> GetByIdAsync(
        AccountId parentId,
        CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageParents, cancellationToken);

        var parent = await WithDetails(db.Parents.AsNoTracking())
            .SingleOrDefaultAsync(p => p.Id == parentId, cancellationToken)
            ?? throw new NotFoundException("Parent not found.");

        return ToResponse(parent);
    }

    public async Task<ParentResponse> LinkStudentAsync(
        AccountId parentId,
        StudentId studentId,
        CancellationToken cancellationToken = default)
    {
        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageParents, cancellationToken);

        var parent = await RequireParentAsync(parentId, cancellationToken);
        var student = (await RequireStudentsAsync([studentId], cancellationToken)).Single();

        var alreadyLinked = await db.ParentStudents
            .AnyAsync(ps => ps.ParentAccountId == parentId && ps.StudentId == studentId, cancellationToken);

        if (alreadyLinked)
        {
            return await GetByIdAsync(parentId, cancellationToken);
        }

        var now = clock.UtcNow;
        db.ParentStudents.Add(new ParentStudent
        {
            ParentAccountId = parentId,
            StudentId = studentId,
            CreatedAtUtc = now
        });

        AddAudit(caller.Id, AuditActions.ParentStudentLinked, parentId,
            $"Linked parent '{parent.UserName}' to student '{student.StudentCode}'.", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(parentId, cancellationToken);
    }

    public async Task<ParentResponse> UnlinkStudentAsync(
        AccountId parentId,
        StudentId studentId,
        CancellationToken cancellationToken = default)
    {
        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageParents, cancellationToken);

        var parent = await RequireParentAsync(parentId, cancellationToken);

        var links = await db.ParentStudents
            .Where(ps => ps.ParentAccountId == parentId)
            .ToListAsync(cancellationToken);

        var link = links.SingleOrDefault(ps => ps.StudentId == studentId);
        if (link is null)
        {
            throw new NotFoundException("That student is not linked to this parent.");
        }

        // "Each parent must be linked to one or more students." Removing the last link would leave
        // an account that can see nothing, so the way to retire a parent is to deactivate them.
        // Swapping one child for another still works: link the new one first, then unlink.
        if (links.Count == 1)
        {
            throw new ConflictException(
                "This is the parent's only linked student. Link another student first, or "
                + "deactivate the parent account instead.");
        }

        var now = clock.UtcNow;
        db.ParentStudents.Remove(link);

        AddAudit(caller.Id, AuditActions.ParentStudentUnlinked, parentId,
            $"Unlinked student {studentId} from parent '{parent.UserName}'.", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(parentId, cancellationToken);
    }

    public async Task<ParentResponse> DeactivateAsync(
        AccountId parentId,
        CancellationToken cancellationToken = default) =>
        await SetActiveAsync(parentId, false, cancellationToken);

    public async Task<ParentResponse> ReactivateAsync(
        AccountId parentId,
        CancellationToken cancellationToken = default) =>
        await SetActiveAsync(parentId, true, cancellationToken);

    private async Task<ParentResponse> SetActiveAsync(
        AccountId parentId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageParents, cancellationToken);

        var parent = await RequireParentAsync(parentId, cancellationToken);

        if (parent.IsActive == isActive)
        {
            return await GetByIdAsync(parentId, cancellationToken);
        }

        var now = clock.UtcNow;
        parent.IsActive = isActive;

        if (!isActive)
        {
            await RevokeActiveTokensAsync(parentId, now, cancellationToken);
        }

        var action = isActive ? AuditActions.AccountReactivated : AuditActions.AccountDeactivated;
        var verb = isActive ? "Reactivated" : "Deactivated";

        AddAudit(caller.Id, action, parentId, $"{verb} parent '{parent.UserName}'.", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(parentId, cancellationToken);
    }

    private async Task<Parent> RequireParentAsync(AccountId parentId, CancellationToken cancellationToken) =>
        await db.Parents.SingleOrDefaultAsync(p => p.Id == parentId, cancellationToken)
        ?? throw new NotFoundException("Parent not found.");

    /// <summary>Resolves every id in one round trip and reports the whole missing set at once,
    /// rather than making the admin discover bad ids one failed call at a time.</summary>
    private async Task<IReadOnlyList<Student>> RequireStudentsAsync(
        IReadOnlyCollection<StudentId> studentIds,
        CancellationToken cancellationToken)
    {
        var found = await db.Students
            .AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var missing = studentIds.Except(found.Select(s => s.Id)).ToArray();
        if (missing.Length > 0)
        {
            throw new NotFoundException($"Student(s) not found: {string.Join(", ", missing)}.");
        }

        var inactive = found.Where(s => !s.IsActive).Select(s => s.StudentCode).Order().ToArray();
        if (inactive.Length > 0)
        {
            throw new ConflictException(
                $"Cannot link an inactive student: {string.Join(", ", inactive)}.");
        }

        return found;
    }

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

    private static IQueryable<Parent> WithDetails(IQueryable<Parent> source) =>
        source
            .Include(p => p.ParentStudents)
            .ThenInclude(ps => ps.Student)
            .ThenInclude(s => s!.Class);

    private void AddAudit(AccountId adminId, string action, AccountId targetId, string details, DateTime now) =>
        db.AuditLogs.Add(new AuditLog
        {
            AdminAccountId = adminId,
            Action = action,
            TargetType = nameof(Parent),
            TargetId = targetId.ToString(),
            Details = details,
            CreatedAtUtc = now
        });

    private static ParentResponse ToResponse(Parent parent) =>
        new(
            parent.Id,
            parent.FullName,
            parent.UserName,
            parent.Email,
            parent.IsActive,
            parent.CreatedAtUtc,
            [.. parent.ParentStudents
                .Where(ps => ps.Student is not null)
                .Select(ps => new ParentStudentResponse(
                    ps.StudentId,
                    ps.Student!.StudentCode,
                    ps.Student.FullName,
                    ps.Student.CurrentClassId,
                    ps.Student.Class?.Name ?? string.Empty,
                    ps.Student.IsActive))
                .OrderBy(s => s.FullName)]);
}
