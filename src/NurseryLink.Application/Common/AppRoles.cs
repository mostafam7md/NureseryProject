using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Common;

public static class AppRoles
{
    public const string Admin = nameof(AccountType.Admin);
    public const string Teacher = nameof(AccountType.Teacher);
    public const string Parent = nameof(AccountType.Parent);
}
