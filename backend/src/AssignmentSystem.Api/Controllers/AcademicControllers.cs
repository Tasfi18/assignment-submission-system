using AssignmentSystem.Api.Extensions;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Contracts.Academics;
using AssignmentSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// Classes (courses). Reading is open to every signed in user because teachers and students need class
/// names for dropdowns and headings; writing is restricted to administrators.
/// </summary>
[ApiController]
[Route("api/classes")]
[Produces("application/json")]
[Authorize]
public class ClassesController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public ClassesController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClassRoomResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClassRoomResponse>>> Get([FromQuery] PageRequest query, CancellationToken cancellationToken)
        => Ok(await _academicService.GetClassRoomsAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClassRoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassRoomResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _academicService.GetClassRoomAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(typeof(ClassRoomResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassRoomResponse>> Create([FromBody] CreateClassRoomRequest request, CancellationToken cancellationToken)
    {
        var created = await _academicService.CreateClassRoomAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(typeof(ClassRoomResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClassRoomResponse>> Update(Guid id, [FromBody] UpdateClassRoomRequest request, CancellationToken cancellationToken)
        => Ok(await _academicService.UpdateClassRoomAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _academicService.DeleteClassRoomAsync(id, cancellationToken);
        return NoContent();
    }
}

/// <summary>Subjects. Same access pattern as classes: everyone reads, admins write.</summary>
[ApiController]
[Route("api/subjects")]
[Produces("application/json")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public SubjectsController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SubjectResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SubjectResponse>>> Get([FromQuery] PageRequest query, CancellationToken cancellationToken)
        => Ok(await _academicService.GetSubjectsAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _academicService.GetSubjectAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<SubjectResponse>> Create([FromBody] CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var created = await _academicService.CreateSubjectAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubjectResponse>> Update(Guid id, [FromBody] UpdateSubjectRequest request, CancellationToken cancellationToken)
        => Ok(await _academicService.UpdateSubjectAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _academicService.DeleteSubjectAsync(id, cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// The mapping "teacher T teaches subject S in class C". Administrators maintain it; a teacher can read
/// back their own mappings to populate the class/subject pickers when creating an assignment.
/// </summary>
[ApiController]
[Route("api/teaching-assignments")]
[Produces("application/json")]
[Authorize]
public class TeachingAssignmentsController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public TeachingAssignmentsController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(typeof(PagedResult<TeachingAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TeachingAssignmentResponse>>> Get(
        [FromQuery] TeachingAssignmentQuery query, CancellationToken cancellationToken)
        => Ok(await _academicService.GetTeachingAssignmentsAsync(query, cancellationToken));

    /// <summary>The class/subject pairs the signed in teacher is allowed to create assignments for.</summary>
    [HttpGet("mine")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.TeacherOnly)]
    [ProducesResponseType(typeof(IReadOnlyList<TeachingAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeachingAssignmentResponse>>> Mine(CancellationToken cancellationToken)
        => Ok(await _academicService.GetMyTeachingAssignmentsAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(typeof(TeachingAssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeachingAssignmentResponse>> Create(
        [FromBody] CreateTeachingAssignmentRequest request, CancellationToken cancellationToken)
    {
        var created = await _academicService.CreateTeachingAssignmentAsync(request, cancellationToken);
        return Created($"/api/teaching-assignments/{created.Id}", created);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _academicService.DeleteTeachingAssignmentAsync(id, cancellationToken);
        return NoContent();
    }
}
