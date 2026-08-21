using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

public class AdminPrivilege
{
    public id AdminAccountId { get; set; }
    public Privilege Privilege { get; set; }

    public Admin? AdminAccount { get; set; }
}
