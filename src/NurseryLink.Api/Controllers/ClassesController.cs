using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryLink.Api.Authorization;
using NurseryLink.Application.Common;
using NurseryLink.Application.Features.Classes;
using NurseryLink.Application.Features.Classes.Dtos;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Api.Controllers;

/// <summary>
/// Classes sit under <see cref="Privilege.ManageStudents"/> rather than
/// <see cref="Privilege.ManageTeachers"/>: the requirements group classes with student records, and
/// reassigning a class is a class-roster decision. One consequence worth knowing is that an admin
/// holding only ManageTeachers cannot free a teacher's class, and so cannot complete a
/// deactivation on their own.
/// </summary>
[ApiController]
[Route("api/classes")]
[Authorize(Roles = AppRoles.Admin)]
[HasPrivilege(Privilege.ManageStudents)]
public sealed class ClassesController(IClassService classService) : ControllerBase
{
    /// <summary>Creates a class, optionally assigning its permanent teacher in the same call.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassResponse>> Create(
        CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var created = await classService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { classId = created.Id }, created);
    }

    /// <summary>Lists classes. Dissolved classes are excluded unless
    /// <paramref name="includeInactive"/> is set.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassResponse>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] bool includeInactive = false)
    {
        return Ok(await classService.GetAllAsync(includeInactive, cancellationToken));
    }

    /// <summary>Gets a class with its current teacher, any substitutes, and its active student count.</summary>
    [HttpGet("{classId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassResponse>> GetById(
        ClassId classId,
        CancellationToken cancellationToken)
    {
        return Ok(await classService.GetByIdAsync(classId, cancellationToken));
    }

    /// <summary>Renames a class.</summary>
    [HttpPatch("{classId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassResponse>> Rename(
        ClassId classId,
        RenameClassRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await classService.RenameAsync(classId, request, cancellationToken));
    }

    /// <summary>Assigns a teacher to the class. A permanent assignment closes the outgoing
    /// teacher's tenure and opens a new one, preserving the full history.</summary>
    [HttpPost("{classId:guid}/teacher")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassResponse>> AssignTeacher(
        ClassId classId,
        AssignTeacherRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await classService.AssignTeacherAsync(classId, request, cancellationToken));
    }

    /// <summary>Ends one open assignment without replacing it.</summary>
    [HttpDelete("{classId:guid}/teachers/{assignmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassResponse>> EndAssignment(
        ClassId classId,
        ClassTeacherId assignmentId,
        CancellationToken cancellationToken)
    {
        return Ok(await classService.EndAssignmentAsync(classId, assignmentId, cancellationToken));
    }

    /// <summary>Returns every teacher tenure this class has had, newest first.</summary>
    [HttpGet("{classId:guid}/teacher-history")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ClassTeacherResponse>>> GetTeacherHistory(
        ClassId classId,
        CancellationToken cancellationToken)
    {
        return Ok(await classService.GetTeacherHistoryAsync(classId, cancellationToken));
    }

    /// <summary>Dissolves a class (soft delete). Rejected with 409 while it still has active
    /// students. The row is kept so historical activity keeps its class context.</summary>
    [HttpPost("{classId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassResponse>> Deactivate(
        ClassId classId,
        CancellationToken cancellationToken)
    {
        return Ok(await classService.DeactivateAsync(classId, cancellationToken));
    }
}
