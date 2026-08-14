using AssignmentSystem.Api.Extensions;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Contracts.Users;
using AssignmentSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>Account management. Every endpoint is restricted to administrators.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Policy = ApiServiceCollectionExtensions.Policies.AdminOnly)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Lists accounts with paging, free-text search and role / class / status filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserResponse>>> Get([FromQuery] UserQuery query, CancellationToken cancellationToken)
        => Ok(await _userService.GetAsync(query, cancellationToken));

    /// <summary>Returns a single account.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _userService.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates an admin, teacher or student. Students must be given a class.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an account. Supplying newPassword also resets the password.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        => Ok(await _userService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Activates or deactivates an account without deleting its history.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> SetStatus(Guid id, [FromQuery] bool isActive, CancellationToken cancellationToken)
        => Ok(await _userService.SetActiveStateAsync(id, isActive, cancellationToken));

    /// <summary>Permanently removes an account. Refused once the account owns assignments or submissions.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
