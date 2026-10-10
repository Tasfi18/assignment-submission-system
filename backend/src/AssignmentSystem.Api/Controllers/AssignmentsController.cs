using AssignmentSystem.Api.Extensions;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Contracts.Assignments;
using AssignmentSystem.Application.Contracts.Submissions;
using AssignmentSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// Assignments.
///
/// The list and detail endpoints are open to all three roles but the result is scoped in the service:
/// admins see everything, teachers see what they created, students see published/closed work for their
/// own class only. Creating and changing an assignment is limited to the owning teacher.
/// </summary>
[ApiController]
[Route("api/assignments")]
[Produces("application/json")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly ISubmissionService _submissionService;

    public AssignmentsController(IAssignmentService assignmentService, ISubmissionService submissionService)
    {
        _assignmentService = assignmentService;
        _submissionService = submissionService;
    }

    /// <summary>Lists assignments visible to the caller, with paging, search and filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentResponse>>> Get(
        [FromQuery] AssignmentQuery query, CancellationToken cancellationToken)
        => Ok(await _assignmentService.GetAsync(query, cancellationToken));

    /// <summary>Returns one assignment, including the caller's own submission when the caller is a student.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _assignmentService.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates an assignment for a class and subject the teacher is assigned to.</summary>
    [HttpPost]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.TeacherOnly)]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AssignmentResponse>> Create(
        [FromBody] CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var created = await _assignmentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an assignment. Only the owning teacher, and never after it has been closed.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.TeacherOnly)]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AssignmentResponse>> Update(
        Guid id, [FromBody] UpdateAssignmentRequest request, CancellationToken cancellationToken)
        => Ok(await _assignmentService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Makes a draft visible to the students of the target class.</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.TeacherOnly)]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AssignmentResponse>> Publish(Guid id, CancellationToken cancellationToken)
        => Ok(await _assignmentService.PublishAsync(id, cancellationToken));

    /// <summary>Stops accepting submissions while keeping the assignment visible.</summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.TeacherOnly)]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AssignmentResponse>> Close(Guid id, CancellationToken cancellationToken)
        => Ok(await _assignmentService.CloseAsync(id, cancellationToken));

    /// <summary>Deletes an assignment. Refused once any student has submitted.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.TeacherOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _assignmentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Lists the submissions received for this assignment (owning teacher or admin).</summary>
    [HttpGet("{id:guid}/submissions")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.TeacherOrAdmin)]
    [ProducesResponseType(typeof(PagedResult<SubmissionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SubmissionResponse>>> GetSubmissions(
        Guid id, [FromQuery] SubmissionQuery query, CancellationToken cancellationToken)
    {
        query.AssignmentId = id;
        return Ok(await _submissionService.GetAsync(query, cancellationToken));
    }

    /// <summary>Submits an answer. Students only, once per assignment.</summary>
    [HttpPost("{id:guid}/submissions")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.StudentOnly)]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubmissionResponse>> Submit(
        Guid id, [FromBody] CreateSubmissionRequest request, CancellationToken cancellationToken)
    {
        var created = await _submissionService.SubmitAsync(id, request, cancellationToken);
        return Created($"/api/submissions/{created.Id}", created);
    }
}
