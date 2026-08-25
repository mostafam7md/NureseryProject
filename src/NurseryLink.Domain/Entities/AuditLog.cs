namespace NurseryLink.Domain.Entities;

public class AuditLog : BaseEntity<AuditLogId>
{
    public AccountId AdminAccountId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? Details { get; set; }

    public Admin? AdminAccount { get; set; }
}
