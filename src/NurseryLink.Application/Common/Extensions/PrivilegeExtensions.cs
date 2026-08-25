using NurseryLink.Domain.Entities;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Common.Extensions;

public static class PrivilegeExtensions
{
    public static IReadOnlyCollection<Privilege> EffectivePrivileges(this Admin admin) =>
        admin.IsSeeded
            ? [.. Enum.GetValues<Privilege>()]
            : [.. admin.Privileges.Select(p => p.Privilege).Distinct()];

    public static string[] PrivilegeNames(this IEnumerable<Privilege> privileges) =>
        [.. privileges.Select(p => p.ToString()).Order()];
}
