namespace NurseryLink.Application.Common.Interfaces;

/// <summary>
/// The nursery's wall clock. Injected rather than calling <see cref="DateTime.UtcNow"/> directly so
/// "same day" logic is testable and honours the configured timezone (PLAN.md A4) instead of UTC.
/// </summary>
public interface INurseryClock
{
    DateTime UtcNow { get; }

    TimeZoneInfo TimeZone { get; }

    /// <summary>The nursery-local calendar date a UTC instant falls on.</summary>
    DateOnly LocalDateOf(DateTime utcInstant);

    DateOnly Today { get; }
}
