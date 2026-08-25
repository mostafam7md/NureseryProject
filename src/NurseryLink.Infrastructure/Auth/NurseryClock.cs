using Microsoft.Extensions.Options;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Infrastructure.Configuration;

namespace NurseryLink.Infrastructure.Auth;

public sealed class NurseryClock : INurseryClock
{
    public NurseryClock(IOptions<NurserySettings> options)
    {
        var id = options.Value.TimeZone;

        // TimeZoneInfo accepts IANA ids on Windows and Windows ids on Linux via ICU, but the
        // mapping can be missing on a trimmed runtime, so surface a clear configuration error
        // rather than a cryptic one at the first write.
        try
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"Configured timezone '{id}' ({NurserySettings.SectionName}:TimeZone) was not found on this machine.",
                exception);
        }
    }

    public TimeZoneInfo TimeZone { get; }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly LocalDateOf(DateTime utcInstant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc),
            TimeZone));

    public DateOnly Today => LocalDateOf(UtcNow);
}
