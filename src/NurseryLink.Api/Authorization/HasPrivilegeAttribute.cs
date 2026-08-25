using Microsoft.AspNetCore.Authorization;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Api.Authorization;

public sealed class HasPrivilegeAttribute(Privilege privilege)
    : AuthorizeAttribute(PrivilegePolicies.PolicyName(privilege))
{
    public Privilege Privilege { get; } = privilege;
}

public static class PrivilegePolicies
{
    public const string Prefix = "Privilege:";

    public static string PolicyName(Privilege privilege) => $"{Prefix}{privilege}";
}
