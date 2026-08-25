namespace NurseryLink.Domain.Entities;

public class Student : BaseEntity<StudentId>
{
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string StudentCode { get; set; } = string.Empty;

    /// <summary>The class the student is in right now. Past activity keeps its own
    /// <see cref="ActivityLog.ClassId"/> snapshot, so transfers only affect future records.</summary>
    public ClassId CurrentClassId { get; set; }

    public bool IsActive { get; set; } = true;

    public Class? Class { get; set; }
    public ICollection<ParentStudent> ParentStudents { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
