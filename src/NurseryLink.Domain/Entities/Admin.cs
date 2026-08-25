namespace NurseryLink.Domain.Entities;

public class Admin : Account
{
    /// <summary>Which admin created this one. Null only for the seeded default admin.</summary>
    public AccountId? CreatedByAdminId { get; set; }

    public Admin? CreatedByAdmin { get; set; }
    public ICollection<Admin> CreatedAdmins { get; set; } = [];
    public ICollection<AdminPrivilege> Privileges { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
