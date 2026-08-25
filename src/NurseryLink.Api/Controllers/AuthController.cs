using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NurseryLink.Api;
using NurseryLink.Application.Features.Auth;
using NurseryLink.Application.Features.Auth.Dtos;

namespace NurseryLink.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Authenticates with username/email + password and returns a short-lived access token
    /// plus a refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await authService.LoginAsync(request, cancellationToken));
    }

    /// <summary>Exchanges a refresh token for a new access token. Re-reads the account's active
    /// state and privileges, so deactivation and privilege changes take effect here.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        return Ok(await authService.RefreshAsync(request, cancellationToken));
    }

    /// <summary>Revokes a refresh token. Any access token already issued remains valid until it
    /// expires, which is why the access-token lifetime is kept short.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns the profile and effective privileges of the authenticated account.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<AccountResponse>> Me(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetMeAsync(cancellationToken));
    }
}
