namespace NurseryLink.Application.Features.Teachers.Dtos;

public sealed record CreateTeacherRequest(
    string FullName,
    string UserName,
    string Email,
    string Password);

/// <summary>A class the teacher holds right now. Ended tenures are not reported here — they are
/// part of the class's history, available from the class endpoints.</summary>
public sealed record TeacherAssignmentResponse(
    ClassTeacherId AssignmentId,
    ClassId ClassId,
    string ClassName,
    string AssignmentType,
    DateTime StartedAtUtc);

public sealed record TeacherResponse(
    AccountId Id,
    string FullName,
    string UserName,
    string Email,
    bool IsActive,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<TeacherAssignmentResponse> CurrentAssignments);
