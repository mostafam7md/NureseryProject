using NurseryLink.Domain.Constants;

namespace NurseryLink.Domain.Entities;

public class Student : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public id CurrentClassId { get; set; }
    public bool IsActive { get; set; } = true;

    public Class? Class { get; set; }
    public ICollection<ParentStudent> ParentStudents { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
