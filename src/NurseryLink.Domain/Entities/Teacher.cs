namespace NurseryLink.Domain.Entities;

public class Teacher : Account
{
    public ICollection<ClassTeacher> ClassTeachers { get; set; } = [];
}
