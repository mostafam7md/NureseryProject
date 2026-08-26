using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryLink.Api.Authorization;
using NurseryLink.Application.Common;
using NurseryLink.Application.Features.Parents;
using NurseryLink.Application.Features.Parents.Dtos;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Api.Controllers;

[ApiController]
[Route("api/parents")]
[Authorize(Roles = AppRoles.Admin)]
[HasPrivilege(Privilege.ManageParents)]
public sealed class ParentsController(IParentService parentService) : ControllerBase
{
    /// <summary>Creates a parent account linked to one or more students.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParentResponse>> Create(
        CreateParentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await parentService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { parentId = created.Id }, created);
    }

    /// <summary>Lists parent accounts and the students each is linked to.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ParentResponse>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] bool includeInactive = false)
    {
        return Ok(await parentService.GetAllAsync(includeInactive, cancellationToken));
    }

    /// <summary>Gets a single parent account with their linked students.</summary>
    [HttpGet("{parentId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParentResponse>> GetById(
        AccountId parentId,
        CancellationToken cancellationToken)
    {
        return Ok(await parentService.GetByIdAsync(parentId, cancellationToken));
    }

    /// <summary>Links an additional student to this parent.</summary>
    [HttpPut("{parentId:guid}/students/{studentId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParentResponse>> LinkStudent(
        AccountId parentId,
        StudentId studentId,
        CancellationToken cancellationToken)
    {
        return Ok(await parentService.LinkStudentAsync(parentId, studentId, cancellationToken));
    }

    /// <summary>Removes a parent-student link. Refuses to remove the last one.</summary>
    [HttpDelete("{parentId:guid}/students/{studentId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParentResponse>> UnlinkStudent(
        AccountId parentId,
        StudentId studentId,
        CancellationToken cancellationToken)
    {
        return Ok(await parentService.UnlinkStudentAsync(parentId, studentId, cancellationToken));
    }

    /// <summary>Deactivates a parent account. Links and history are preserved.</summary>
    [HttpPost("{parentId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParentResponse>> Deactivate(
        AccountId parentId,
        CancellationToken cancellationToken)
    {
        return Ok(await parentService.DeactivateAsync(parentId, cancellationToken));
    }

    /// <summary>Restores a previously deactivated parent account.</summary>
    [HttpPost("{parentId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParentResponse>> Reactivate(
        AccountId parentId,
        CancellationToken cancellationToken)
    {
        return Ok(await parentService.ReactivateAsync(parentId, cancellationToken));
    }
}
