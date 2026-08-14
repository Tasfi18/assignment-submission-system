using AssignmentSystem.Application.Contracts.Auth;
using AssignmentSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>Sign in and account self-service.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Exchanges an email and password for a JWT access token.</summary>
    /// <response code="200">Authentication succeeded.</response>
    /// <response code="401">The email or password is wrong.</response>
    /// <response code="403">The account is deactivated, or the student portal is switched off.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Returns the profile of the signed in account.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummary>> Me(CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(cancellationToken);
        return Ok(user);
    }

    /// <summary>Changes the password of the signed in account.</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }
}
