using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

public class ActivityLog : BaseEntity<ActivityLogId>
{
    public StudentId StudentId { get; set; }

    /// <summary>Snapshot of the class the student was in when this was logged. Transfers change the
    /// student's current class but must never rewrite history, so this is copied at write time
    /// rather than read through <see cref="Student.CurrentClassId"/>.</summary>
    public ClassId ClassId { get; set; }

    public AccountId LoggedByAccountId { get; set; }

    public ActivityLogType LogType { get; set; }

    /// <summary>
    /// JSON body whose schema depends on <see cref="LogType"/>:
    /// <list type="bullet">
    /// <item><c>Meal</c>: <c>{ "mealType": "Breakfast|Lunch|Snack", "status": "AteAll|AteSome|Refused" }</c></item>
    /// <item><c>Toilet</c>: <c>{ "visitType": "Pee|Poo|Both", "note": "..." }</c></item>
    /// <item><c>Temperature</c>: <c>{ "celsius": 37.5, "isFever": false, "note": "..." }</c></item>
    /// </list>
    /// Because the body is opaque to the database, the rules that depend on its contents
    /// (one meal type per child per day, temperature within 34.0-42.5 °C) are enforced in the
    /// service layer only — there is no index or CHECK constraint backing them up.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    public DateTime LoggedAtUtc { get; set; }

    /// <summary>
    /// <see cref="LoggedAtUtc"/> projected into the nursery's local timezone and reduced to a date.
    /// Persisted (rather than computed on read) so "same day" queries and the one-meal-per-day check
    /// stay correct across the UTC offset — at UTC+3 a 21:30 local entry is already tomorrow in UTC.
    /// </summary>
    public DateOnly LocalDate { get; set; }

    public Student? Student { get; set; }
    public Class? Class { get; set; }
    public Account? LoggedByAccount { get; set; }
}
