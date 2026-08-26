using Microsoft.EntityFrameworkCore;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Common.Services;

/// <summary>
/// Creates a notification about a student and fans it out to every linked parent.
/// </summary>
/// <remarks>
/// "Notifications are never created manually — they are always triggered by something that happens
/// in the system." There is deliberately no admin-facing entry point: this is only reachable from
/// the code path of the event that caused it.
/// <para>
/// Rows are added to the change tracker but not saved. The caller saves, so the notification and
/// the record that triggered it land in the same transaction — either both exist or neither does.
/// </para>
/// </remarks>
public interface INotificationDispatcher
{
    /// <returns>How many parents the notification was addressed to.</returns>
    Task<int> QueueForStudentAsync(
        StudentId studentId,
        NotificationType type,
        string message,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}

public sealed class NotificationDispatcher(IApplicationDbContext db) : INotificationDispatcher
{
    public async Task<int> QueueForStudentAsync(
        StudentId studentId,
        NotificationType type,
        string message,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        // Every linked parent, deactivated or not: the links are what determine who receives a
        // notification. Skipping inactive accounts would drop the alert permanently rather than
        // defer it, because the fan-out is only ever computed here — a parent deactivated for a day
        // would never learn their child had a fever. They cannot sign in to read it early anyway,
        // so the inbox query filters at read time instead.
        var parentIds = await db.ParentStudents
            .Where(ps => ps.StudentId == studentId)
            .Select(ps => ps.ParentAccountId)
            .ToListAsync(cancellationToken);

        var notification = new Notification
        {
            StudentId = studentId,
            Type = type,
            Message = message,
            CreatedAtUtc = nowUtc
        };

        db.Notifications.Add(notification);

        // A student with no linked parents still gets the Notification row: the event happened and
        // the admin dashboard reads it. Only the per-parent inbox fan-out is empty.
        db.ParentNotifications.AddRange(parentIds.Select(parentId => new ParentNotification
        {
            NotificationId = notification.Id,
            ParentAccountId = parentId,
            IsRead = false
        }));

        return parentIds.Count;
    }
}
