namespace NurseryLink.Application.Features.Parents.Dtos;

/// <summary>"Each parent must be linked to one or more students", so the links are supplied up
/// front rather than left to a follow-up call that might never happen.</summary>
public sealed record CreateParentRequest(
    string FullName,
    string UserName,
    string Email,
    string Password,
    IReadOnlyCollection<StudentId> StudentIds);

public sealed record ParentStudentResponse(
    StudentId StudentId,
    string StudentCode,
    string FullName,
    ClassId CurrentClassId,
    string CurrentClassName,
    bool IsActive);

public sealed record ParentResponse(
    AccountId Id,
    string FullName,
    string UserName,
    string Email,
    bool IsActive,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<ParentStudentResponse> Students);
