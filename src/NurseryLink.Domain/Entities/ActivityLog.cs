using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public id StudentId { get; set; }
    public id ClassId { get; set; }
    public id LoggedByAccountId { get; set; }
    public ActivityLogType LogType { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime LoggedAtUtc { get; set; }

    public Student? Student { get; set; }
    public Class? Class { get; set; }
    public Account? LoggedByAccount { get; set; }
}
