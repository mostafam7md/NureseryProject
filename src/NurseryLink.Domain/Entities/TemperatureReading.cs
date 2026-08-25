using NurseryLink.Domain.Constants;

namespace NurseryLink.Domain.Entities;

public class TemperatureReading : BaseEntity
{
    public id StudentId { get; set; }
    public id ClassId { get; set; }
    public decimal ValueCelsius { get; set; }
    public string? Note { get; set; }
    public id TeacherUserId { get; set; }
    public DateTime LoggedAtUtc { get; set; }

    public Student? Student { get; set; }
    public Class? Class { get; set; }
    public Account? TeacherUser { get; set; }
}
