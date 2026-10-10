using System.ComponentModel.DataAnnotations;
using AssignmentSystem.Application.Common.Models;

namespace AssignmentSystem.Application.Contracts.Academics;

public record ClassRoomResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public int AcademicYear { get; init; }

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public int StudentCount { get; init; }
}

public record CreateClassRoomRequest
{
    [Required(ErrorMessage = "Class name is required.")]
    [MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Class code is required.")]
    [MaxLength(30)]
    public string Code { get; init; } = string.Empty;

    [Range(2000, 2100, ErrorMessage = "Academic year must be between 2000 and 2100.")]
    public int AcademicYear { get; init; } = DateTime.UtcNow.Year;

    [MaxLength(500)]
    public string? Description { get; init; }
}

public record UpdateClassRoomRequest : CreateClassRoomRequest
{
    public bool IsActive { get; init; } = true;
}

public record SubjectResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }
}

public record CreateSubjectRequest
{
    [Required(ErrorMessage = "Subject name is required.")]
    [MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Subject code is required.")]
    [MaxLength(30)]
    public string Code { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }
}

public record UpdateSubjectRequest : CreateSubjectRequest
{
    public bool IsActive { get; init; } = true;
}

public record TeachingAssignmentResponse
{
    public Guid Id { get; init; }

    public Guid ClassRoomId { get; init; }

    public string ClassRoomName { get; init; } = string.Empty;

    public Guid SubjectId { get; init; }

    public string SubjectName { get; init; } = string.Empty;

    public Guid TeacherId { get; init; }

    public string TeacherName { get; init; } = string.Empty;

    public string TeacherEmail { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}

public record CreateTeachingAssignmentRequest
{
    [Required]
    public Guid ClassRoomId { get; init; }

    [Required]
    public Guid SubjectId { get; init; }

    [Required]
    public Guid TeacherId { get; init; }
}

public class TeachingAssignmentQuery : PageRequest
{
    public Guid? ClassRoomId { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? TeacherId { get; set; }
}
