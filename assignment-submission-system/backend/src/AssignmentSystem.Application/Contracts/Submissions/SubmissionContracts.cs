using System.ComponentModel.DataAnnotations;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Contracts.Submissions;

public record SubmissionResponse
{
    public Guid Id { get; init; }

    public Guid AssignmentId { get; init; }

    public string AssignmentTitle { get; init; } = string.Empty;

    public string SubjectName { get; init; } = string.Empty;

    public string ClassRoomName { get; init; } = string.Empty;

    public decimal AssignmentMaxMarks { get; init; }

    public DateTime AssignmentDeadlineUtc { get; init; }

    public Guid StudentId { get; init; }

    public string StudentName { get; init; } = string.Empty;

    public string StudentEmail { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string? AttachmentUrl { get; init; }

    public DateTime SubmittedAtUtc { get; init; }

    public DateTime? LastUpdatedAtUtc { get; init; }

    public bool IsLate { get; init; }

    public int RevisionCount { get; init; }

    public SubmissionStatus Status { get; init; }

    public string StatusName { get; init; } = string.Empty;

    public decimal? Marks { get; init; }

    public string? Feedback { get; init; }

    public string? GradedByName { get; init; }

    public DateTime? GradedAtUtc { get; init; }
}

public record CreateSubmissionRequest
{
    [Required(ErrorMessage = "An answer is required.")]
    [MinLength(1)]
    [MaxLength(20000)]
    public string Content { get; init; } = string.Empty;

    [MaxLength(500)]
    [Url(ErrorMessage = "The attachment must be a valid URL.")]
    public string? AttachmentUrl { get; init; }
}

public record UpdateSubmissionRequest : CreateSubmissionRequest
{
}

public record GradeSubmissionRequest
{
    [Required(ErrorMessage = "Marks are required.")]
    [Range(0, 1000, ErrorMessage = "Marks cannot be negative.")]
    public decimal Marks { get; init; }

    [MaxLength(2000)]
    public string? Feedback { get; init; }
}

public record UpdateSubmissionStatusRequest
{
    [Required]
    [EnumDataType(typeof(SubmissionStatus))]
    public SubmissionStatus Status { get; init; }

    [MaxLength(2000)]
    public string? Feedback { get; init; }
}

public class SubmissionQuery : PageRequest
{
    public Guid? AssignmentId { get; set; }

    public Guid? StudentId { get; set; }

    public Guid? ClassRoomId { get; set; }

    public SubmissionStatus? Status { get; set; }

    public bool? IsLate { get; set; }
}
