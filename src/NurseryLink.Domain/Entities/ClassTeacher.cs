namespace NurseryLink.Domain.Entities;

public class ClassTeacher
{
    public id ClassId { get; set; }
    public id TeacherAccountId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public Class? Class { get; set; }
    public Teacher? TeacherAccount { get; set; }
}
