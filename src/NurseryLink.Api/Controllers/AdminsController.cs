using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryLink.Api.Authorization;
using NurseryLink.Application.Common;
using NurseryLink.Application.Features.Admins;
using NurseryLink.Application.Features.Admins.Dtos;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Api.Controllers;

[ApiController]
[Route("api/admins")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminsController(IAdminService adminService) : ControllerBase
{
    /// <summary>Creates a new admin account with privileges (subset of the caller's own privileges).</summary>
    [HttpPost]
    [HasPrivilege(Privilege.ManageAdmins)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminResponse>> Create(CreateAdminRequest request, CancellationToken cancellationToken)
    {
        var created = await adminService.CreateAsync(request, cancellationToken);

        // The route value key must match the parameter name in the target action ("adminId").
        // Passing "id" here fails link generation and throws after the admin has been committed.
        return CreatedAtAction(nameof(GetById), new { adminId = created.Id }, created);
    }

    /// <summary>Lists all admin accounts.</summary>
    [HttpGet]
    [HasPrivilege(Privilege.ManageAdmins)]
    public async Task<ActionResult<IReadOnlyList<AdminResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await adminService.GetAllAsync(cancellationToken));
    }

    /// <summary>Gets a single admin account by id.</summary>
    [HttpGet("{adminId:guid}")]
    [HasPrivilege(Privilege.ManageAdmins)]
    public async Task<ActionResult<AdminResponse>> GetById(AccountId adminId, CancellationToken cancellationToken)
    {
        return Ok(await adminService.GetByIdAsync(adminId, cancellationToken));
    }
}
