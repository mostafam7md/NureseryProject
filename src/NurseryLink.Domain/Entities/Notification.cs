using NurseryLink.Domain.Constants;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

public class Notification : BaseEntity
{
    public id StudentId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;

    public Student? Student { get; set; }
    public ICollection<ParentNotification> ParentNotifications { get; set; } = [];
}
