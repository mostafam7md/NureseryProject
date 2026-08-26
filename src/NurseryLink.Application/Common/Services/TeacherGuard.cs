using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Domain.Entities;

namespace NurseryLink.Application.Common.Services;

/// <summary>The student a teacher is allowed to act on, plus the class that access came through.
/// The class is carried out of the check so callers stamp records with the class the teacher
/// actually holds rather than re-reading it.</summary>
public sealed record TeacherStudentScope(Teacher Teacher, Student Student, ClassId ClassId);

/// <summary>
/// Enforces "a teacher can only interact with students in their own assigned class".
/// </summary>
/// <remarks>
/// The scope is derived from the token's subject and the current <see cref="ClassTeacher"/> rows,
/// never from anything in the request body. A teacher can therefore name any student id they like
/// and still only reach their own class.
/// </remarks>
public interface ITeacherGuard
{
    Task<Teacher> RequireTeacherAsync(CancellationToken cancellationToken = default);

    /// <param name="mustBeEnrolled">
    /// True when the caller is about to write a record, which a withdrawn child must not receive.
    /// False for reads: a teacher keeps access to the history of a child who has since left, and
    /// refusing that would hide records they wrote themselves.
    /// </param>
    Task<TeacherStudentScope> RequireStudentInOwnClassAsync(
        StudentId studentId,
        bool mustBeEnrolled = true,
        CancellationToken cancellationToken = default);
}

public sealed class TeacherGuard(IApplicationDbContext db, ICurrentUser currentUser) : ITeacherGuard
{
    public async Task<Teacher> RequireTeacherAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        if (!currentUser.IsTeacher)
        {
            throw new ForbiddenException("Only teachers may perform this action.");
        }

        var teacher = await db.Teachers
            .SingleOrDefaultAsync(t => t.Id == userId, cancellationToken)
            ?? throw new ForbiddenException("Caller is not a teacher account.");

        if (!teacher.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated.");
        }

        return teacher;
    }

    public async Task<TeacherStudentScope> RequireStudentInOwnClassAsync(
        StudentId studentId,
        bool mustBeEnrolled = true,
        CancellationToken cancellationToken = default)
    {
        var teacher = await RequireTeacherAsync(cancellationToken);

        var student = await db.Students
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        if (mustBeEnrolled && !student.IsActive)
        {
            throw new ConflictException(
                $"Student '{student.StudentCode}' is not currently enrolled.");
        }

        // Only open tenures count. A teacher who used to hold this class keeps the records they
        // wrote but must not be able to add new ones.
        var holdsClass = await db.ClassTeachers.AnyAsync(
            a => a.TeacherAccountId == teacher.Id
                 && a.ClassId == student.CurrentClassId
                 && a.EndedAtUtc == null,
            cancellationToken);

        if (!holdsClass)
        {
            throw new ForbiddenException("That student is not in a class you are assigned to.");
        }

        return new TeacherStudentScope(teacher, student, student.CurrentClassId);
    }
}
