using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common.Exceptions;
using NurseryLink.Application.Common.Extensions;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Application.Common.Services;
using NurseryLink.Application.Features.Activities.Dtos;
using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Features.Activities;

public interface IActivityLogService
{
    Task<ActivityLogResponse> LogMealAsync(StudentId studentId, LogMealRequest request, CancellationToken cancellationToken = default);

    Task<ActivityLogResponse> LogToiletVisitAsync(StudentId studentId, LogToiletVisitRequest request, CancellationToken cancellationToken = default);

    Task<ActivityLogResponse> LogTemperatureAsync(StudentId studentId, LogTemperatureRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityLogResponse>> GetForStudentAsync(StudentId studentId, DateOnly? localDate, CancellationToken cancellationToken = default);
}

public sealed class ActivityLogService(
    IApplicationDbContext db,
    ITeacherGuard teacherGuard,
    INotificationDispatcher notifications,
    INurseryClock clock,
    IValidator<LogMealRequest> mealValidator,
    IValidator<LogToiletVisitRequest> toiletValidator,
    IValidator<LogTemperatureRequest> temperatureValidator) : IActivityLogService
{
    /// <summary>Payload JSON is written and read only by this service, so the options are fixed
    /// here rather than taken from the API's serializer — a change to the HTTP contract must not
    /// silently change the shape of rows already in the table.</summary>
    internal static readonly JsonSerializerOptions PayloadJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<ActivityLogResponse> LogMealAsync(
        StudentId studentId,
        LogMealRequest request,
        CancellationToken cancellationToken = default)
    {
        await mealValidator.ValidateOrThrowAsync(request, cancellationToken);

        var scope = await teacherGuard.RequireStudentInOwnClassAsync(studentId, mustBeEnrolled: true, cancellationToken);

        var now = clock.UtcNow;
        var localDate = clock.LocalDateOf(now);

        await EnsureMealNotAlreadyLoggedAsync(studentId, localDate, request.MealType, cancellationToken);

        var entry = NewEntry(scope, ActivityLogType.Meal, now, localDate,
            new MealPayload(request.MealType, request.Status));

        db.ActivityLogs.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return await GetEntryAsync(entry.Id, cancellationToken);
    }

    public async Task<ActivityLogResponse> LogToiletVisitAsync(
        StudentId studentId,
        LogToiletVisitRequest request,
        CancellationToken cancellationToken = default)
    {
        await toiletValidator.ValidateOrThrowAsync(request, cancellationToken);

        var scope = await teacherGuard.RequireStudentInOwnClassAsync(studentId, mustBeEnrolled: true, cancellationToken);

        var now = clock.UtcNow;

        // No per-day limit here, by requirement: "There is no limit on how many visits can be
        // logged per child per day."
        var entry = NewEntry(scope, ActivityLogType.Toilet, now, clock.LocalDateOf(now),
            new ToiletPayload(request.VisitType, Trim(request.Note)));

        db.ActivityLogs.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return await GetEntryAsync(entry.Id, cancellationToken);
    }

    /// <summary>
    /// Records a temperature reading and, if it indicates a fever, notifies every linked parent.
    /// The notification rows are added to the same <c>SaveChanges</c> as the reading, so a failure
    /// cannot leave a fever recorded with nobody told, or parents told about a reading that was
    /// never stored.
    /// </summary>
    public async Task<ActivityLogResponse> LogTemperatureAsync(
        StudentId studentId,
        LogTemperatureRequest request,
        CancellationToken cancellationToken = default)
    {
        await temperatureValidator.ValidateOrThrowAsync(request, cancellationToken);

        var scope = await teacherGuard.RequireStudentInOwnClassAsync(studentId, mustBeEnrolled: true, cancellationToken);

        if (request.Celsius < NurseryConstants.MinTemperatureCelsius ||
            request.Celsius > NurseryConstants.MaxTemperatureCelsius)
        {
            throw new UnprocessableEntityException(
                $"A temperature reading must be between {NurseryConstants.MinTemperatureCelsius} and "
                + $"{NurseryConstants.MaxTemperatureCelsius} °C. Received {request.Celsius}.");
        }

        var isFever = request.Celsius > NurseryConstants.FeverThresholdCelsius;
        var now = clock.UtcNow;

        var entry = NewEntry(scope, ActivityLogType.Temperature, now, clock.LocalDateOf(now),
            new TemperaturePayload(request.Celsius, isFever, Trim(request.Note)));

        db.ActivityLogs.Add(entry);

        if (isFever)
        {
            // Automatic, with no action from the teacher: "the system must automatically notify
            // all parents linked to that child without any manual action from the teacher."
            await notifications.QueueForStudentAsync(
                studentId,
                NotificationType.Fever,
                $"{scope.Student.FullName} recorded a temperature of {request.Celsius} °C, "
                + $"above the {NurseryConstants.FeverThresholdCelsius} °C fever threshold.",
                now,
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        return await GetEntryAsync(entry.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ActivityLogResponse>> GetForStudentAsync(
        StudentId studentId,
        DateOnly? localDate,
        CancellationToken cancellationToken = default)
    {
        await teacherGuard.RequireStudentInOwnClassAsync(studentId, mustBeEnrolled: false, cancellationToken);

        var query = WithDetails(db.ActivityLogs.AsNoTracking())
            .Where(e => e.StudentId == studentId);

        if (localDate is { } date)
        {
            query = query.Where(e => e.LocalDate == date);
        }

        var entries = await query
            .OrderByDescending(e => e.LoggedAtUtc)
            .ToListAsync(cancellationToken);

        return [.. entries.Select(ToResponse)];
    }

    /// <summary>
    /// "It must not be possible to log the same meal type twice for the same child on the same
    /// day." The day is the nursery-local one (PLAN.md A4), which is why <c>LocalDate</c> is
    /// persisted rather than derived from <c>LoggedAtUtc</c> at read time.
    /// </summary>
    /// <remarks>
    /// Per PLAN.md A14 this is a read-then-write with no unique index behind it, because the meal
    /// type lives inside the JSON payload. Two simultaneous requests for the same meal can both
    /// pass. Closing that needs a persisted discriminator column or the TPH split the plan
    /// describes; until then this holds for the one-teacher-at-a-time reality of a nursery room.
    /// </remarks>
    private async Task EnsureMealNotAlreadyLoggedAsync(
        StudentId studentId,
        DateOnly localDate,
        MealType mealType,
        CancellationToken cancellationToken)
    {
        // The index on (StudentId, LocalDate, LogType) makes this a seek; the payload match is done
        // in memory because the database cannot look inside the JSON string.
        var payloads = await db.ActivityLogs
            .AsNoTracking()
            .Where(e => e.StudentId == studentId
                        && e.LocalDate == localDate
                        && e.LogType == ActivityLogType.Meal)
            .Select(e => e.Payload)
            .ToListAsync(cancellationToken);

        var alreadyLogged = payloads
            .Select(Deserialize<MealPayload>)
            .Any(p => p?.MealType == mealType);

        if (alreadyLogged)
        {
            throw new ConflictException(
                $"{mealType} has already been logged for this child on {localDate:yyyy-MM-dd}.");
        }
    }

    private ActivityLog NewEntry<TPayload>(
        TeacherStudentScope scope,
        ActivityLogType logType,
        DateTime nowUtc,
        DateOnly localDate,
        TPayload payload) =>
        new()
        {
            StudentId = scope.Student.Id,
            // Snapshot, not a lookup: a later transfer must not rewrite which class this happened in.
            ClassId = scope.ClassId,
            LoggedByAccountId = scope.Teacher.Id,
            LogType = logType,
            Payload = JsonSerializer.Serialize(payload, PayloadJson),
            LoggedAtUtc = nowUtc,
            LocalDate = localDate,
            CreatedAtUtc = nowUtc
        };

    private async Task<ActivityLogResponse> GetEntryAsync(ActivityLogId id, CancellationToken cancellationToken)
    {
        var entry = await WithDetails(db.ActivityLogs.AsNoTracking())
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException("Activity entry not found.");

        return ToResponse(entry);
    }

    private static IQueryable<ActivityLog> WithDetails(IQueryable<ActivityLog> source) =>
        source
            .Include(e => e.Student)
            .Include(e => e.Class)
            .Include(e => e.LoggedByAccount);

    private static string? Trim(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    private static T? Deserialize<T>(string payload)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload, PayloadJson);
        }
        catch (JsonException)
        {
            // A row written by an older payload shape should not take down a whole day's read.
            return null;
        }
    }

    private static ActivityLogResponse ToResponse(ActivityLog entry) =>
        new(
            entry.Id,
            entry.StudentId,
            entry.Student?.FullName ?? string.Empty,
            entry.ClassId,
            entry.Class?.Name ?? string.Empty,
            entry.LogType.ToString(),
            entry.LoggedByAccountId,
            entry.LoggedByAccount?.FullName ?? string.Empty,
            entry.LoggedAtUtc,
            entry.LocalDate,
            entry.LogType == ActivityLogType.Meal ? Deserialize<MealPayload>(entry.Payload) : null,
            entry.LogType == ActivityLogType.Toilet ? Deserialize<ToiletPayload>(entry.Payload) : null,
            entry.LogType == ActivityLogType.Temperature ? Deserialize<TemperaturePayload>(entry.Payload) : null);
}
