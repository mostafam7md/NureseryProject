namespace NurseryLink.Domain.Entities;

public class ParentStudent
{
    public id ParentAccountId { get; set; }
    public id StudentId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Parent? ParentAccount { get; set; }
    public Student? Student { get; set; }
}
