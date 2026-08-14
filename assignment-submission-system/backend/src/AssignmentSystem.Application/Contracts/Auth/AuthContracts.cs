using System.ComponentModel.DataAnnotations;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Contracts.Auth;

public record LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [MaxLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public DateTime ExpiresAtUtc { get; init; }

    public UserSummary User { get; init; } = null!;
}

public record UserSummary
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public string RoleName { get; init; } = string.Empty;

    public Guid? ClassRoomId { get; init; }

    public string? ClassRoomName { get; init; }
}

public record ChangePasswordRequest
{
    [Required]
    [MaxLength(128)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "The new password must be at least 6 characters.")]
    [MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;
}
