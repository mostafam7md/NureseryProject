using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Extensions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Application.Common.Services;
using NurseryLink.Application.Features.Classes.Dtos;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Classes;

public interface IClassService
{
    Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<ClassResponse> GetByIdAsync(ClassId classId, CancellationToken cancellationToken = default);

    Task<ClassResponse> RenameAsync(ClassId classId, RenameClassRequest request, CancellationToken cancellationToken = default);

    Task<ClassResponse> AssignTeacherAsync(ClassId classId, AssignTeacherRequest request, CancellationToken cancellationToken = default);

    Task<ClassResponse> EndAssignmentAsync(ClassId classId, ClassTeacherId assignmentId, CancellationToken cancellationToken = default);

    Task<ClassResponse> DeactivateAsync(ClassId classId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassTeacherResponse>> GetTeacherHistoryAsync(ClassId classId, CancellationToken cancellationToken = default);
}

public sealed class ClassService(
    IApplicationDbContext db,
    IAdminGuard adminGuard,
    INurseryClock clock,
    IValidator<CreateClassRequest> createValidator,
    IValidator<RenameClassRequest> renameValidator,
    IValidator<AssignTeacherRequest> assignValidator) : IClassService
{
    public async Task<ClassResponse> CreateAsync(
        CreateClassRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, null, cancellationToken);

        var now = clock.UtcNow;
        var classEntity = new Class
        {
            Name = name,
            IsActive = true,
            CreatedAtUtc = now
        };

        db.Classes.Add(classEntity);
        AddAudit(caller.Id, AuditActions.ClassCreated, classEntity.Id, $"Created class '{name}'.", now);

        // Assigning at creation time is a plain insert — there is no outgoing tenure to close, so
        // unlike AssignTeacherAsync this needs no transaction.
        if (request.TeacherAccountId is { } teacherId)
        {
            var teacher = await RequireAssignableTeacherAsync(teacherId, cancellationToken);

            db.ClassTeachers.Add(new ClassTeacher
            {
                ClassId = classEntity.Id,
                TeacherAccountId = teacher.Id,
                AssignmentType = AssignmentType.Permanent,
                StartedAtUtc = now,
                CreatedAtUtc = now
            });

            AddAudit(caller.Id, AuditActions.ClassTeacherAssigned, classEntity.Id,
                $"Assigned teacher '{teacher.UserName}' to class '{name}' as Permanent.", now);
        }

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(classEntity.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassResponse>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var classes = await WithCurrentTeachers(db.Classes.AsNoTracking())
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var counts = await ActiveStudentCountsAsync(cancellationToken);

        return [.. classes.Select(c => ToResponse(c, counts.GetValueOrDefault(c.Id)))];
    }

    public async Task<ClassResponse> GetByIdAsync(
        ClassId classId,
        CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var classEntity = await WithCurrentTeachers(db.Classes.AsNoTracking())
            .SingleOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Class not found.");

        var count = await db.Students
            .CountAsync(s => s.IsActive && s.CurrentClassId == classId, cancellationToken);

        return ToResponse(classEntity, count);
    }

    public async Task<ClassResponse> RenameAsync(
        ClassId classId,
        RenameClassRequest request,
        CancellationToken cancellationToken = default)
    {
        await renameValidator.ValidateOrThrowAsync(request, cancellationToken);

        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var classEntity = await db.Classes.SingleOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Class not found.");

        var name = request.Name.Trim();
        if (string.Equals(classEntity.Name, name, StringComparison.Ordinal))
        {
            return await GetByIdAsync(classId, cancellationToken);
        }

        await EnsureNameIsFreeAsync(name, classId, cancellationToken);

        var now = clock.UtcNow;
        var previousName = classEntity.Name;
        classEntity.Name = name;

        AddAudit(caller.Id, AuditActions.ClassRenamed, classId,
            $"Renamed class '{previousName}' to '{name}'.", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(classId, cancellationToken);
    }

    /// <summary>
    /// Moves a class to a different teacher. The outgoing tenure is closed rather than deleted, so
    /// the assignment history — and therefore the context of everything the previous teacher logged
    /// — survives the change intact (PLAN.md A2).
    /// </summary>
    public async Task<ClassResponse> AssignTeacherAsync(
        ClassId classId,
        AssignTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        await assignValidator.ValidateOrThrowAsync(request, cancellationToken);

        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var classEntity = await db.Classes.SingleOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Class not found.");

        if (!classEntity.IsActive)
        {
            throw new ConflictException("This class has been dissolved and cannot take a teacher.");
        }

        var teacher = await RequireAssignableTeacherAsync(request.TeacherAccountId, cancellationToken);

        var current = await db.ClassTeachers
            .Where(a => a.ClassId == classId && a.EndedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (current.Any(a => a.TeacherAccountId == teacher.Id && a.AssignmentType == request.AssignmentType))
        {
            throw new ConflictException(
                $"'{teacher.UserName}' already holds this class as {request.AssignmentType}.");
        }

        var now = clock.UtcNow;

        // The close-out and the new row cannot share one SaveChanges: the filtered unique index
        // allows a single open permanent tenure per class, and SQL Server evaluates it per
        // statement. If EF ordered the INSERT before the UPDATE the batch would fail, so the two
        // steps are sequenced explicitly and made atomic by the transaction instead.
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);

        if (request.AssignmentType == AssignmentType.Permanent)
        {
            foreach (var outgoing in current.Where(a => a.AssignmentType == AssignmentType.Permanent))
            {
                outgoing.EndedAtUtc = CloseOutAt(outgoing.StartedAtUtc, now);

                AddAudit(caller.Id, AuditActions.ClassTeacherUnassigned, classId,
                    $"Ended permanent assignment {outgoing.Id} on class '{classEntity.Name}'.", now);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        db.ClassTeachers.Add(new ClassTeacher
        {
            ClassId = classId,
            TeacherAccountId = teacher.Id,
            AssignmentType = request.AssignmentType,
            StartedAtUtc = now,
            CreatedAtUtc = now
        });

        AddAudit(caller.Id, AuditActions.ClassTeacherAssigned, classId,
            $"Assigned teacher '{teacher.UserName}' to class '{classEntity.Name}' as {request.AssignmentType}.", now);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(classId, cancellationToken);
    }

    /// <summary>Ends one open tenure without replacing it — the way a substitute's cover is closed,
    /// and the way a permanent teacher is released before their account is deactivated.</summary>
    public async Task<ClassResponse> EndAssignmentAsync(
        ClassId classId,
        ClassTeacherId assignmentId,
        CancellationToken cancellationToken = default)
    {
        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var classEntity = await db.Classes.SingleOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Class not found.");

        // Matching on both ids stops an assignment being ended through the wrong class's route.
        var assignment = await db.ClassTeachers
            .SingleOrDefaultAsync(a => a.Id == assignmentId && a.ClassId == classId, cancellationToken)
            ?? throw new NotFoundException("Assignment not found for this class.");

        if (assignment.EndedAtUtc is not null)
        {
            return await GetByIdAsync(classId, cancellationToken);
        }

        var now = clock.UtcNow;
        assignment.EndedAtUtc = CloseOutAt(assignment.StartedAtUtc, now);

        AddAudit(caller.Id, AuditActions.ClassTeacherUnassigned, classId,
            $"Ended {assignment.AssignmentType} assignment {assignment.Id} on class '{classEntity.Name}'.", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(classId, cancellationToken);
    }

    /// <summary>Soft-deletes a class (PLAN.md O3). The row stays so historical activity keeps a
    /// resolvable class context.</summary>
    public async Task<ClassResponse> DeactivateAsync(
        ClassId classId,
        CancellationToken cancellationToken = default)
    {
        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var classEntity = await db.Classes.SingleOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Class not found.");

        if (!classEntity.IsActive)
        {
            return await GetByIdAsync(classId, cancellationToken);
        }

        // A student's CurrentClassId is required, so dissolving a class under them would leave
        // them pointing at a class that no longer runs. Move them out first.
        var remaining = await db.Students
            .CountAsync(s => s.IsActive && s.CurrentClassId == classId, cancellationToken);

        if (remaining > 0)
        {
            throw new ConflictException(
                $"This class still has {remaining} active student(s). Transfer them to another "
                + "class before dissolving it.");
        }

        var now = clock.UtcNow;
        classEntity.IsActive = false;

        var current = await db.ClassTeachers
            .Where(a => a.ClassId == classId && a.EndedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var assignment in current)
        {
            assignment.EndedAtUtc = CloseOutAt(assignment.StartedAtUtc, now);
        }

        AddAudit(caller.Id, AuditActions.ClassDeactivated, classId,
            $"Dissolved class '{classEntity.Name}' and ended {current.Count} teacher assignment(s).", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(classId, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassTeacherResponse>> GetTeacherHistoryAsync(
        ClassId classId,
        CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        if (!await db.Classes.AnyAsync(c => c.Id == classId, cancellationToken))
        {
            throw new NotFoundException("Class not found.");
        }

        var assignments = await db.ClassTeachers
            .AsNoTracking()
            .Include(a => a.TeacherAccount)
            .Where(a => a.ClassId == classId)
            .OrderByDescending(a => a.StartedAtUtc)
            .ToListAsync(cancellationToken);

        return [.. assignments.Select(ToResponse)];
    }

    /// <summary>
    /// The CK_ClassTeachers_EndsAfterStart constraint is a strict greater-than. Assigning and
    /// reassigning inside the same clock tick is unlikely but would otherwise fail the write, so a
    /// same-instant close-out is nudged one tick past the start.
    /// </summary>
    private static DateTime CloseOutAt(DateTime startedAtUtc, DateTime now) =>
        now > startedAtUtc ? now : startedAtUtc.AddTicks(1);

    private async Task<Teacher> RequireAssignableTeacherAsync(
        AccountId teacherId,
        CancellationToken cancellationToken)
    {
        var teacher = await db.Teachers
            .SingleOrDefaultAsync(t => t.Id == teacherId, cancellationToken)
            ?? throw new NotFoundException("Teacher not found.");

        if (!teacher.IsActive)
        {
            throw new ConflictException(
                $"'{teacher.UserName}' has been deactivated and cannot be assigned to a class.");
        }

        return teacher;
    }

    private async Task EnsureNameIsFreeAsync(string name, ClassId? excluding, CancellationToken cancellationToken)
    {
        var query = db.Classes.Where(c => c.Name == name);

        if (excluding is { } currentId)
        {
            query = query.Where(c => c.Id != currentId);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            // The unique index covers dissolved classes too, so the clash may be with a class that
            // is no longer running. Say so, otherwise the 409 looks like a bug.
            throw new ConflictException(
                $"A class named '{name}' already exists. Class names stay reserved even after a "
                + "class is dissolved, so historical records remain unambiguous.");
        }
    }

    /// <summary>Counts active students per class in one grouped pass. Deliberately not filtered to
    /// the classes being listed: an <c>IN</c> over value-converted ids buys nothing at nursery
    /// scale and only adds a translation edge case.</summary>
    private async Task<Dictionary<ClassId, int>> ActiveStudentCountsAsync(CancellationToken cancellationToken) =>
        await db.Students
            .Where(s => s.IsActive)
            .GroupBy(s => s.CurrentClassId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassId, x => x.Count, cancellationToken);

    private static IQueryable<Class> WithCurrentTeachers(IQueryable<Class> source) =>
        source
            .Include(c => c.ClassTeachers.Where(a => a.EndedAtUtc == null))
            .ThenInclude(a => a.TeacherAccount);

    private void AddAudit(AccountId adminId, string action, ClassId targetId, string details, DateTime now) =>
        db.AuditLogs.Add(new AuditLog
        {
            AdminAccountId = adminId,
            Action = action,
            TargetType = nameof(Class),
            TargetId = targetId.ToString(),
            Details = details,
            CreatedAtUtc = now
        });

    private static ClassResponse ToResponse(Class classEntity, int activeStudentCount)
    {
        var current = classEntity.ClassTeachers.Where(a => a.EndedAtUtc is null).ToList();

        return new ClassResponse(
            classEntity.Id,
            classEntity.Name,
            classEntity.IsActive,
            classEntity.CreatedAtUtc,
            current
                .Where(a => a.AssignmentType == AssignmentType.Permanent)
                .Select(ToResponse)
                .SingleOrDefault(),
            [.. current
                .Where(a => a.AssignmentType == AssignmentType.Substitute)
                .OrderBy(a => a.StartedAtUtc)
                .Select(ToResponse)],
            activeStudentCount);
    }

    private static ClassTeacherResponse ToResponse(ClassTeacher assignment) =>
        new(
            assignment.Id,
            assignment.TeacherAccountId,
            assignment.TeacherAccount?.FullName ?? string.Empty,
            assignment.TeacherAccount?.UserName ?? string.Empty,
            assignment.AssignmentType.ToString(),
            assignment.StartedAtUtc,
            assignment.EndedAtUtc);
}
