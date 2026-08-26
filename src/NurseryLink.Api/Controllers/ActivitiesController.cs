using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NurseryLink.Application.Common;
using NurseryLink.Application.Features.Activities;
using NurseryLink.Application.Features.Activities.Dtos;

namespace NurseryLink.Api.Controllers;

/// <summary>
/// Daily logging, written by the teacher who holds the child's class.
/// </summary>
/// <remarks>
/// The route carries the student, never the teacher or the class: both are taken from the token and
/// the current class assignment, so an entry can only ever be attributed to whoever actually sent
/// it. A student outside the caller's class is refused regardless of what the route says.
/// </remarks>
[ApiController]
[Route("api/students/{studentId:guid}/activity")]
[Authorize(Roles = AppRoles.Teacher)]
public sealed class ActivitiesController(IActivityLogService activityService) : ControllerBase
{
    /// <summary>Records a meal. One entry per meal type per child per nursery-local day.</summary>
    [HttpPost("meals")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivityLogResponse>> LogMeal(
        StudentId studentId,
        LogMealRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await activityService.LogMealAsync(studentId, request, cancellationToken));
    }

    /// <summary>Records a toilet visit. No per-day limit.</summary>
    [HttpPost("toilet-visits")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityLogResponse>> LogToiletVisit(
        StudentId studentId,
        LogToiletVisitRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await activityService.LogToiletVisitAsync(studentId, request, cancellationToken));
    }

    /// <summary>Records a temperature reading. Out-of-range values are refused with 422; a
    /// feverish reading notifies every linked parent automatically.</summary>
    [HttpPost("temperatures")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ActivityLogResponse>> LogTemperature(
        StudentId studentId,
        LogTemperatureRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await activityService.LogTemperatureAsync(studentId, request, cancellationToken));
    }

    /// <summary>Lists a child's entries, newest first. Pass date to narrow to one nursery-local day.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ActivityLogResponse>>> GetForStudent(
        StudentId studentId,
        CancellationToken cancellationToken,
        [FromQuery] DateOnly? date = null)
    {
        return Ok(await activityService.GetForStudentAsync(studentId, date, cancellationToken));
    }
}
