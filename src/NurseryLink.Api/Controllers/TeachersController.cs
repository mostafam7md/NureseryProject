using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryLink.Api.Authorization;
using NurseryLink.Application.Common;
using NurseryLink.Application.Features.Teachers;
using NurseryLink.Application.Features.Teachers.Dtos;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Api.Controllers;

[ApiController]
[Route("api/teachers")]
[Authorize(Roles = AppRoles.Admin)]
[HasPrivilege(Privilege.ManageTeachers)]
public sealed class TeachersController(ITeacherService teacherService) : ControllerBase
{
    /// <summary>Creates a teacher account. The class assignment is made separately through
    /// <c>POST /api/classes/{classId}/teacher</c>.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeacherResponse>> Create(
        CreateTeacherRequest request,
        CancellationToken cancellationToken)
    {
        var created = await teacherService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { teacherId = created.Id }, created);
    }

    /// <summary>Lists teacher accounts. Deactivated teachers are excluded unless
    /// <paramref name="includeInactive"/> is set.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeacherResponse>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] bool includeInactive = false)
    {
        return Ok(await teacherService.GetAllAsync(includeInactive, cancellationToken));
    }

    /// <summary>Gets a single teacher account together with the classes they currently hold.</summary>
    [HttpGet("{teacherId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherResponse>> GetById(
        AccountId teacherId,
        CancellationToken cancellationToken)
    {
        return Ok(await teacherService.GetByIdAsync(teacherId, cancellationToken));
    }

    /// <summary>Deactivates a teacher who has left. Rejected with 409 while they still hold a
    /// class — reassign it first. Everything they logged stays intact.</summary>
    [HttpPost("{teacherId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeacherResponse>> Deactivate(
        AccountId teacherId,
        CancellationToken cancellationToken)
    {
        return Ok(await teacherService.DeactivateAsync(teacherId, cancellationToken));
    }

    /// <summary>Restores a previously deactivated teacher account.</summary>
    [HttpPost("{teacherId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherResponse>> Reactivate(
        AccountId teacherId,
        CancellationToken cancellationToken)
    {
        return Ok(await teacherService.ReactivateAsync(teacherId, cancellationToken));
    }
}
