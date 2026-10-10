using AssignmentSystem.Api.Extensions;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Contracts.Settings;
using AssignmentSystem.Application.Contracts.Submissions;
using AssignmentSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// Submissions. Results are scoped by role in the service: a student only ever sees their own work,
/// a teacher only the work handed in for the assignments they own, and an admin sees everything.
/// </summary>
[ApiController]
[Route("api/submissions")]
[Produces("application/json")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    public SubmissionsController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    /// <summary>Lists submissions visible to the caller, with paging and filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SubmissionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SubmissionResponse>>> Get(
        [FromQuery] SubmissionQuery query, CancellationToken cancellationToken)
        => Ok(await _submissionService.GetAsync(query, cancellationToken));

    /// <summary>Convenience endpoint for the student portal: the signed in student's own submissions.</summary>
    [HttpGet("mine")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.StudentOnly)]
    [ProducesResponseType(typeof(PagedResult<SubmissionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SubmissionResponse>>> Mine(
        [FromQuery] SubmissionQuery query, CancellationToken cancellationToken)
        => Ok(await _submissionService.GetAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _submissionService.GetByIdAsync(id, cancellationToken));

    /// <summary>Replaces the answer of an existing submission. Owning student only, and never after grading.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.StudentOnly)]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubmissionResponse>> Update(
        Guid id, [FromBody] UpdateSubmissionRequest request, CancellationToken cancellationToken)
        => Ok(await _submissionService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Awards marks and feedback. Restricted to the teacher who owns the assignment.</summary>
    [HttpPost("{id:guid}/grade")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.TeacherOnly)]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SubmissionResponse>> Grade(
        Guid id, [FromBody] GradeSubmissionRequest request, CancellationToken cancellationToken)
        => Ok(await _submissionService.GradeAsync(id, request, cancellationToken));

    /// <summary>Moves a submission through the review workflow (under review, returned for revision, rejected).</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = ApiServiceCollectionExtensions.Policies.TeacherOnly)]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubmissionResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateSubmissionStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _submissionService.UpdateStatusAsync(id, request, cancellationToken));
}

/// <summary>Application level switches maintained by the administrator.</summary>
[ApiController]
[Route("api/settings")]
[Produces("application/json")]
[Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AppSettingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppSettingResponse>>> Get(CancellationToken cancellationToken)
        => Ok(await _settingsService.GetAllAsync(cancellationToken));

    /// <summary>Updates one setting, for example switching late submissions off for the whole institution.</summary>
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(AppSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppSettingResponse>> Update(
        string key, [FromBody] UpdateSettingRequest request, CancellationToken cancellationToken)
        => Ok(await _settingsService.UpdateAsync(key, request, cancellationToken));
}

/// <summary>Aggregated counts for the administrator's landing page.</summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
[Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
public class DashboardController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public DashboardController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
        => Ok(await _settingsService.GetDashboardAsync(cancellationToken));
}
