using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using NurseryLink.Application.Common;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Api.Authorization;

/// <summary>
/// Materialises one policy per <see cref="Privilege"/> on demand, so <c>[HasPrivilege(...)]</c>
/// works without registering five policies by hand (and without them drifting when the enum grows).
/// </summary>
public sealed class PrivilegePolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    // Built policies are immutable and identical per privilege; caching avoids rebuilding one on
    // every authorized request. The key space is bounded by the enum, so this cannot grow.
    private static readonly ConcurrentDictionary<Privilege, AuthorizationPolicy> Cache = new();

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PrivilegePolicies.Prefix, StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<Privilege>(policyName[PrivilegePolicies.Prefix.Length..], ignoreCase: true, out var privilege) &&
            Enum.IsDefined(privilege))
        {
            return Cache.GetOrAdd(privilege, static p => new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Admin)
                .AddRequirements(new PrivilegeRequirement(p))
                .Build());
        }

        return await base.GetPolicyAsync(policyName);
    }
}
