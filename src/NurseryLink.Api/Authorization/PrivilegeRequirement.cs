using Microsoft.AspNetCore.Authorization;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Api.Authorization;

public sealed record PrivilegeRequirement(Privilege Privilege) : IAuthorizationRequirement;
