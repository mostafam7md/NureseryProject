using NurseryLink.Domain.Constants;

namespace NurseryLink.Domain.Entities;

public class AuditLog : BaseEntity
{
    public id AdminAccountId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? Details { get; set; }

    public Admin? AdminAccount { get; set; }
}
