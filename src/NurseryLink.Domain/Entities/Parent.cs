namespace NurseryLink.Domain.Entities;

public class Parent : Account
{
    public ICollection<ParentStudent> ParentStudents { get; set; } = [];
    public ICollection<ParentNotification> Notifications { get; set; } = [];
}
