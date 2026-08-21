using NurseryLink.Domain.Constants;

namespace NurseryLink.Domain.Entities;

public class Class : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ClassTeacher> ClassTeachers { get; set; } = [];
    public ICollection<Student> Students { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
