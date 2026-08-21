namespace NurseryLink.Domain.Entities;

public abstract class BaseEntity
{
    public id Id { get; set; } = id.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
