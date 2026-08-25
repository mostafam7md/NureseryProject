using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

public class ToiletVisit : BaseEntity
{
    public id StudentId { get; set; }
    public id ClassId { get; set; }
    public ToiletVisitType VisitType { get; set; }
    public string? Note { get; set; }
    public id TeacherUserId { get; set; }
    public DateTime LoggedAtUtc { get; set; }

    public Student? Student { get; set; }
    public Class? Class { get; set; }
    public Account? TeacherUser { get; set; }
}
