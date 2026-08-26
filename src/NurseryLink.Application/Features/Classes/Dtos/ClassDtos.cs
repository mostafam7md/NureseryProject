using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Classes.Dtos;

/// <summary>Creating a class may assign its permanent teacher in the same call, so a class need
/// never exist in an unstaffed state.</summary>
public sealed record CreateClassRequest(
    string Name,
    AccountId? TeacherAccountId);

public sealed record RenameClassRequest(string Name);

public sealed record AssignTeacherRequest(
    AccountId TeacherAccountId,
    AssignmentType AssignmentType);

/// <summary>One teacher's tenure over the class. <see cref="EndedAtUtc"/> is null while current.</summary>
public sealed record ClassTeacherResponse(
    ClassTeacherId AssignmentId,
    AccountId TeacherAccountId,
    string TeacherFullName,
    string TeacherUserName,
    string AssignmentType,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc);

public sealed record ClassResponse(
    ClassId Id,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc,
    ClassTeacherResponse? CurrentTeacher,
    IReadOnlyCollection<ClassTeacherResponse> CurrentSubstitutes,
    int ActiveStudentCount);
