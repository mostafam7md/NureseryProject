namespace NurseryLink.Domain.Entities;

public class Class : BaseEntity<ClassId>
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Soft delete. Dissolved classes stay in the table so historical activity keeps its
    /// original class context (PLAN.md O3).</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<ClassTeacher> ClassTeachers { get; set; } = [];
    public ICollection<Student> Students { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
