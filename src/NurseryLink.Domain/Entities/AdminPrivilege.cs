using NurseryLink.Domain.Enums;

namespace NurseryLink.Domain.Entities;

public class AdminPrivilege
{
    public AccountId AdminAccountId { get; set; }
    public Privilege Privilege { get; set; }

    public Admin? AdminAccount { get; set; }
}
