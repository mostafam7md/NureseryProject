using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryLink.Api.Authorization;
using NurseryLink.Application.Common;
using NurseryLink.Application.Features.Students;
using NurseryLink.Application.Features.Students.Dtos;
using NurseryLink.Domain.Enums;

namespace NurseryLink.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = AppRoles.Admin)]
[HasPrivilege(Privilege.ManageStudents)]
public sealed class StudentsController(IStudentService studentService) : ControllerBase
{
    /// <summary>Creates a student record. Omit studentCode to have one allocated.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentResponse>> Create(
        CreateStudentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await studentService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { studentId = created.Id }, created);
    }

    /// <summary>Lists students, optionally filtered to one class.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentResponse>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] ClassId? classId = null,
        [FromQuery] bool includeInactive = false)
    {
        return Ok(await studentService.GetAllAsync(classId, includeInactive, cancellationToken));
    }

    /// <summary>Gets a student with their current class and linked parents.</summary>
    [HttpGet("{studentId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentResponse>> GetById(
        StudentId studentId,
        CancellationToken cancellationToken)
    {
        return Ok(await studentService.GetByIdAsync(studentId, cancellationToken));
    }

    /// <summary>Updates a student name or date of birth.</summary>
    [HttpPatch("{studentId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentResponse>> Update(
        StudentId studentId,
        UpdateStudentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await studentService.UpdateAsync(studentId, request, cancellationToken));
    }

    /// <summary>Moves a student to a different class. Affects future activity only — records
    /// already written keep the class they were logged in.</summary>
    [HttpPost("{studentId:guid}/transfer")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentResponse>> Transfer(
        StudentId studentId,
        TransferStudentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await studentService.TransferAsync(studentId, request, cancellationToken));
    }

    /// <summary>Marks a student as no longer enrolled. Their records are preserved.</summary>
    [HttpPost("{studentId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentResponse>> Deactivate(
        StudentId studentId,
        CancellationToken cancellationToken)
    {
        return Ok(await studentService.DeactivateAsync(studentId, cancellationToken));
    }

    /// <summary>Re-enrols a previously deactivated student.</summary>
    [HttpPost("{studentId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentResponse>> Reactivate(
        StudentId studentId,
        CancellationToken cancellationToken)
    {
        return Ok(await studentService.ReactivateAsync(studentId, cancellationToken));
    }
}
