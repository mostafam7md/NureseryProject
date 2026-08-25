using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

public class MealEntry : BaseEntity
{
    public id StudentId { get; set; }
    public id ClassId { get; set; }
    public MealType MealType { get; set; }
    public EatingStatus EatingStatus { get; set; }
    public id TeacherUserId { get; set; }
    public DateTime LoggedAtUtc { get; set; }
    public DateOnly LocalDate { get; set; }

    public Student? Student { get; set; }
    public Class? Class { get; set; }
    public Account? TeacherUser { get; set; }
}
