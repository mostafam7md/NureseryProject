using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

/// <summary>
/// A system-generated event about a student. Never created manually. Fanned out to each linked
/// parent through <see cref="ParentNotification"/>, which carries the per-parent read state.
/// </summary>
public class Notification : BaseEntity<NotificationId>
{
    public StudentId StudentId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;

    public Student? Student { get; set; }
    public ICollection<ParentNotification> ParentNotifications { get; set; } = [];
}
