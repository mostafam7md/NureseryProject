namespace NurseryLink.Domain.Entities;

public class ParentNotification
{
    public id NotificationId { get; set; }
    public id ParentAccountId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public Notification? Notification { get; set; }
    public Parent? ParentAccount { get; set; }
}
