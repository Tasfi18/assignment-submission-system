using System.ComponentModel.DataAnnotations;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Contracts.Users;

public record UserResponse
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public string RoleName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public Guid? ClassRoomId { get; init; }

    public string? ClassRoomName { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public record CreateUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [EnumDataType(typeof(UserRole), ErrorMessage = "Role must be Admin, Teacher or Student.")]
    public UserRole Role { get; init; }

    /// <summary>Required when <see cref="Role"/> is Student, ignored otherwise.</summary>
    public Guid? ClassRoomId { get; init; }
}

public record UpdateUserRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    public Guid? ClassRoomId { get; init; }

    /// <summary>Optional. When supplied the password is reset to this value.</summary>
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [MaxLength(128)]
    public string? NewPassword { get; init; }
}

public class UserQuery : PageRequest
{
    public UserRole? Role { get; set; }

    public Guid? ClassRoomId { get; set; }

    public bool? IsActive { get; set; }
}
