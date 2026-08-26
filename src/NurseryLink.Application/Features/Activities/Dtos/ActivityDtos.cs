using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Activities.Dtos;

// ---- requests ---------------------------------------------------------------------------------
// None of these carry the teacher, the class or the timestamp: all three are server-derived, so a
// client cannot log on someone else's behalf or back-date an entry.

public sealed record LogMealRequest(MealType MealType, EatingStatus Status);

public sealed record LogToiletVisitRequest(ToiletVisitType VisitType, string? Note);

public sealed record LogTemperatureRequest(decimal Celsius, string? Note);

// ---- payloads ---------------------------------------------------------------------------------
// The persisted shape of ActivityLog.Payload. Exactly one is populated on a response, matching
// LogType — keeping them as separate typed properties means OpenAPI can describe them, which a
// raw JSON blob could not.

public sealed record MealPayload(MealType MealType, EatingStatus Status);

public sealed record ToiletPayload(ToiletVisitType VisitType, string? Note);

public sealed record TemperaturePayload(decimal Celsius, bool IsFever, string? Note);

// ---- responses --------------------------------------------------------------------------------

public sealed record ActivityLogResponse(
    ActivityLogId Id,
    StudentId StudentId,
    string StudentName,
    // The class the entry was written in, not the class the student is in today.
    ClassId ClassId,
    string ClassName,
    string LogType,
    AccountId LoggedByAccountId,
    string LoggedByName,
    DateTime LoggedAtUtc,
    DateOnly LocalDate,
    MealPayload? Meal,
    ToiletPayload? Toilet,
    TemperaturePayload? Temperature);
