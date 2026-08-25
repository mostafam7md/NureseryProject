namespace NurseryLink.Domain.Entities;

public class ParentStudent
{
    public AccountId ParentAccountId { get; set; }
    public StudentId StudentId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Parent? ParentAccount { get; set; }
    public Student? Student { get; set; }
}
