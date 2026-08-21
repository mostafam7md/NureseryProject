namespace NurseryLink.Domain.Entities;

public class Admin : Account
{
    public id? CreatedByAdminId { get; set; }

    public Admin? CreatedByAdmin { get; set; }
    public ICollection<Admin> CreatedAdmins { get; set; } = [];
    public ICollection<AdminPrivilege> Privileges { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
