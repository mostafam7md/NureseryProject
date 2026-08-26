namespace NurseryLink.Domain.Constants;

public static class NurseryConstants
{
    public const decimal MinTemperatureCelsius = 34.0m;
    public const decimal MaxTemperatureCelsius = 42.5m;
    public const decimal FeverThresholdCelsius = 38.0m;

    public const int FullNameMaxLength = 100;
    public const int UserNameMaxLength = 50;
    public const int EmailMaxLength = 254;
    public const int PasswordHashMaxLength = 500;
    public const int ClassNameMaxLength = 50;
    public const int StudentCodeMaxLength = 20;

    /// <summary>Upper bound used to sanity-check a student date of birth.</summary>
    public const int MaxStudentAgeYears = 12;

    /// <summary>Prefix for system-allocated student codes: NUR-2026-0001.</summary>
    public const string StudentCodePrefix = "NUR";
    public const int NoteMaxLength = 500;
    public const int MessageMaxLength = 500;
    public const int ActionMaxLength = 100;
    public const int TargetTypeMaxLength = 50;
    public const int TargetIdMaxLength = 100;
    public const int DetailsMaxLength = 2000;

    /// <summary>Cap on the ActivityLog JSON body so the hottest table does not carry an
    /// unbounded LOB column.</summary>
    public const int ActivityPayloadMaxLength = 1000;

    /// <summary>SHA-256 rendered as lowercase hex.</summary>
    public const int TokenHashLength = 64;

    /// <summary>Long enough for an IPv6 address with an embedded IPv4 suffix.</summary>
    public const int IpAddressMaxLength = 45;
}
