using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

/// <summary>
/// One teacher's tenure over one class. Reassignment stamps <see cref="EndedAtUtc"/> on the current
/// row and inserts a new one, so the full history survives and a teacher can hold the same class
/// again later — neither of which the old composite (ClassId, TeacherAccountId) key allowed.
/// </summary>
public class ClassTeacher : BaseEntity<ClassTeacherId>
{
    public ClassId ClassId { get; set; }

    public AccountId TeacherAccountId { get; set; }

    public AssignmentType AssignmentType { get; set; } = AssignmentType.Permanent;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Null while the assignment is current. A filtered unique index on
    /// <c>ClassId WHERE EndedAtUtc IS NULL AND AssignmentType = 0</c> is what actually guarantees
    /// the "exactly one teacher at a time" rule.</summary>
    public DateTime? EndedAtUtc { get; set; }

    public Class? Class { get; set; }
    public Teacher? TeacherAccount { get; set; }

    public bool IsCurrent => EndedAtUtc is null;
}
