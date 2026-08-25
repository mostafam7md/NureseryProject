using System.ComponentModel.DataAnnotations;

namespace NurseryLink.Infrastructure.Configuration;

public sealed class NurserySettings
{
    public const string SectionName = "Nursery";

    /// <summary>
    /// IANA (or Windows) timezone the nursery operates in. Daily rules — one meal type per child
    /// per day, "today's activity" — are evaluated against this, not UTC (PLAN.md A4).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string TimeZone { get; init; } = "Africa/Cairo";
}
