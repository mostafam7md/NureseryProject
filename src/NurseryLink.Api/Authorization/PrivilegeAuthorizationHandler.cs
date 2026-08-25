using Microsoft.AspNetCore.Authorization;
using NurseryLink.Application.Common;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Api.Authorization;

public sealed class PrivilegeAuthorizationHandler : AuthorizationHandler<PrivilegeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PrivilegeRequirement requirement)
    {
        if (Enum.IsDefined(requirement.Privilege) &&
            context.User.HasClaim(AppClaims.Privilege, requirement.Privilege.ToString()))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
