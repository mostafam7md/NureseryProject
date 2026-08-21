using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

public abstract class Account : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSeeded { get; set; }

    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
