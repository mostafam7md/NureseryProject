namespace NurseryLink.Application.Features.Students.Dtos;

/// <summary>Leave <see cref="StudentCode"/> null to have the system allocate one (PLAN.md A10).</summary>
public sealed record CreateStudentRequest(
    string FullName,
    DateOnly DateOfBirth,
    ClassId ClassId,
    string? StudentCode);

public sealed record UpdateStudentRequest(
    string FullName,
    DateOnly DateOfBirth);

public sealed record TransferStudentRequest(ClassId ClassId);

public sealed record StudentParentResponse(
    AccountId ParentAccountId,
    string FullName,
    string UserName,
    string Email,
    bool IsActive);

public sealed record StudentResponse(
    StudentId Id,
    string StudentCode,
    string FullName,
    DateOnly DateOfBirth,
    bool IsActive,
    DateTime CreatedAtUtc,
    ClassId CurrentClassId,
    string CurrentClassName,
    IReadOnlyCollection<StudentParentResponse> Parents);
