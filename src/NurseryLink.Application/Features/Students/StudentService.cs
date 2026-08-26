using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Extensions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Application.Common.Services;
using NurseryLink.Application.Features.Students.Dtos;
using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Students;

public interface IStudentService
{
    Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentResponse>> GetAllAsync(ClassId? classId, bool includeInactive, CancellationToken cancellationToken = default);

    Task<StudentResponse> GetByIdAsync(StudentId studentId, CancellationToken cancellationToken = default);

    Task<StudentResponse> UpdateAsync(StudentId studentId, UpdateStudentRequest request, CancellationToken cancellationToken = default);

    Task<StudentResponse> TransferAsync(StudentId studentId, TransferStudentRequest request, CancellationToken cancellationToken = default);

    Task<StudentResponse> DeactivateAsync(StudentId studentId, CancellationToken cancellationToken = default);

    Task<StudentResponse> ReactivateAsync(StudentId studentId, CancellationToken cancellationToken = default);
}

public sealed class StudentService(
    IApplicationDbContext db,
    IAdminGuard adminGuard,
    INurseryClock clock,
    IValidator<CreateStudentRequest> createValidator,
    IValidator<UpdateStudentRequest> updateValidator,
    IValidator<TransferStudentRequest> transferValidator) : IStudentService
{
    public async Task<StudentResponse> CreateAsync(
        CreateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);
        var classEntity = await RequireEnrollableClassAsync(request.ClassId, cancellationToken);

        var now = clock.UtcNow;
        var code = request.StudentCode?.Trim().ToUpperInvariant()
                   ?? await AllocateStudentCodeAsync(now, cancellationToken);

        if (await db.Students.AnyAsync(s => s.StudentCode == code, cancellationToken))
        {
            throw new ConflictException($"A student with code '{code}' already exists.");
        }

        var student = new Student
        {
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            StudentCode = code,
            CurrentClassId = classEntity.Id,
            IsActive = true,
            CreatedAtUtc = now
        };

        db.Students.Add(student);
        AddAudit(caller.Id, AuditActions.StudentCreated, student.Id,
            $"Created student '{student.FullName}' ({code}) in class '{classEntity.Name}'.", now);

        // Same pattern as AdminService: the pre-check above is for a readable message, the unique
        // index on StudentCode is the actual guarantee. Two creates racing on the same allocated
        // number both pass the pre-check, and the loser gets a 409 from the index rather than a
        // duplicate code. Retrying the request allocates the next number.
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(student.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<StudentResponse>> GetAllAsync(
        ClassId? classId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var query = WithDetails(db.Students.AsNoTracking())
            .Where(s => includeInactive || s.IsActive);

        if (classId is { } id)
        {
            query = query.Where(s => s.CurrentClassId == id);
        }

        var students = await query.OrderBy(s => s.FullName).ToListAsync(cancellationToken);

        return [.. students.Select(ToResponse)];
    }

    public async Task<StudentResponse> GetByIdAsync(
        StudentId studentId,
        CancellationToken cancellationToken = default)
    {
        await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var student = await WithDetails(db.Students.AsNoTracking())
            .SingleOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        return ToResponse(student);
    }

    public async Task<StudentResponse> UpdateAsync(
        StudentId studentId,
        UpdateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateOrThrowAsync(request, cancellationToken);

        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var student = await db.Students.SingleOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        var now = clock.UtcNow;
        student.FullName = request.FullName.Trim();
        student.DateOfBirth = request.DateOfBirth;

        AddAudit(caller.Id, AuditActions.StudentUpdated, studentId,
            $"Updated student '{student.FullName}' ({student.StudentCode}).", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(studentId, cancellationToken);
    }

    /// <summary>
    /// Moves a student to a different class. Only <see cref="Student.CurrentClassId"/> changes —
    /// every existing <see cref="ActivityLog"/> keeps the <c>ClassId</c> it was written with, so
    /// the transfer affects future activity only and history stays attached to the class it
    /// happened in (PLAN.md A3).
    /// </summary>
    public async Task<StudentResponse> TransferAsync(
        StudentId studentId,
        TransferStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        await transferValidator.ValidateOrThrowAsync(request, cancellationToken);

        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var student = await db.Students.SingleOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        if (student.CurrentClassId == request.ClassId)
        {
            return await GetByIdAsync(studentId, cancellationToken);
        }

        var destination = await RequireEnrollableClassAsync(request.ClassId, cancellationToken);

        var origin = await db.Classes
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == student.CurrentClassId, cancellationToken);

        var now = clock.UtcNow;
        student.CurrentClassId = destination.Id;

        AddAudit(caller.Id, AuditActions.StudentTransferred, studentId,
            $"Transferred student '{student.FullName}' ({student.StudentCode}) from "
            + $"'{origin?.Name ?? "unknown"}' to '{destination.Name}'. Existing records keep their "
            + "original class.", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(studentId, cancellationToken);
    }

    public async Task<StudentResponse> DeactivateAsync(
        StudentId studentId,
        CancellationToken cancellationToken = default) =>
        await SetActiveAsync(studentId, false, cancellationToken);

    public async Task<StudentResponse> ReactivateAsync(
        StudentId studentId,
        CancellationToken cancellationToken = default) =>
        await SetActiveAsync(studentId, true, cancellationToken);

    private async Task<StudentResponse> SetActiveAsync(
        StudentId studentId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var caller = await adminGuard.RequireAdminAsync(Privilege.ManageStudents, cancellationToken);

        var student = await db.Students.SingleOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        if (student.IsActive == isActive)
        {
            return await GetByIdAsync(studentId, cancellationToken);
        }

        // Reactivating into a dissolved class would leave the student pointing at a class that no
        // longer runs, which is the same hole DeactivateAsync on the class refuses to open.
        if (isActive)
        {
            await RequireEnrollableClassAsync(student.CurrentClassId, cancellationToken);
        }

        var now = clock.UtcNow;
        student.IsActive = isActive;

        // Deliberately not the Account* actions: a student has no login, and reusing those strings
        // would make a report counting deactivated accounts silently include withdrawn children.
        var action = isActive ? AuditActions.StudentReadmitted : AuditActions.StudentWithdrawn;
        var verb = isActive ? "Readmitted" : "Withdrew";

        AddAudit(caller.Id, action, studentId,
            $"{verb} student '{student.FullName}' ({student.StudentCode}).", now);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(studentId, cancellationToken);
    }

    /// <summary>
    /// Allocates the next <c>NUR-{year}-{0001}</c> code. Reads the highest number currently in use
    /// for the year rather than counting rows, so deleting or renaming a record cannot make the
    /// allocator hand out a number twice.
    /// </summary>
    private async Task<string> AllocateStudentCodeAsync(DateTime now, CancellationToken cancellationToken)
    {
        var year = clock.LocalDateOf(now).Year;
        var prefix = $"{NurseryConstants.StudentCodePrefix}-{year}-";

        var existing = await db.Students
            .AsNoTracking()
            .Where(s => s.StudentCode.StartsWith(prefix))
            .Select(s => s.StudentCode)
            .ToListAsync(cancellationToken);

        var highest = existing
            .Select(code => int.TryParse(code[prefix.Length..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{highest + 1:D4}";
    }

    private async Task<Class> RequireEnrollableClassAsync(ClassId classId, CancellationToken cancellationToken)
    {
        var classEntity = await db.Classes
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Class not found.");

        if (!classEntity.IsActive)
        {
            throw new ConflictException(
                $"Class '{classEntity.Name}' has been dissolved and cannot take students.");
        }

        return classEntity;
    }

    private static IQueryable<Student> WithDetails(IQueryable<Student> source) =>
        source
            .Include(s => s.Class)
            .Include(s => s.ParentStudents)
            .ThenInclude(ps => ps.ParentAccount);

    private void AddAudit(AccountId adminId, string action, StudentId targetId, string details, DateTime now) =>
        db.AuditLogs.Add(new AuditLog
        {
            AdminAccountId = adminId,
            Action = action,
            TargetType = nameof(Student),
            TargetId = targetId.ToString(),
            Details = details,
            CreatedAtUtc = now
        });

    private static StudentResponse ToResponse(Student student) =>
        new(
            student.Id,
            student.StudentCode,
            student.FullName,
            student.DateOfBirth,
            student.IsActive,
            student.CreatedAtUtc,
            student.CurrentClassId,
            student.Class?.Name ?? string.Empty,
            [.. student.ParentStudents
                .Where(ps => ps.ParentAccount is not null)
                .Select(ps => new StudentParentResponse(
                    ps.ParentAccountId,
                    ps.ParentAccount!.FullName,
                    ps.ParentAccount.UserName,
                    ps.ParentAccount.Email,
                    ps.ParentAccount.IsActive))
                .OrderBy(p => p.FullName)]);
}
