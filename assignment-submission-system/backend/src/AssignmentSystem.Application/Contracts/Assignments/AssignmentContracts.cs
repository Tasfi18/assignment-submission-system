using System.ComponentModel.DataAnnotations;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Contracts.Assignments;

public record AssignmentResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid ClassRoomId { get; init; }

    public string ClassRoomName { get; init; } = string.Empty;

    public Guid SubjectId { get; init; }

    public string SubjectName { get; init; } = string.Empty;

    public Guid TeacherId { get; init; }

    public string TeacherName { get; init; } = string.Empty;

    public decimal MaxMarks { get; init; }

    public DateTime DeadlineUtc { get; init; }

    public AssignmentStatus Status { get; init; }

    public string StatusName { get; init; } = string.Empty;

    public bool AllowLateSubmission { get; init; }

    public bool AllowResubmission { get; init; }

    public DateTime? PublishedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public bool IsPastDeadline { get; init; }

    /// <summary>Number of submissions received. Always populated for teachers/admins.</summary>
    public int SubmissionCount { get; init; }

    /// <summary>Number of students in the target class (helps teachers see "12 of 30 submitted").</summary>
    public int ClassStudentCount { get; init; }

    /// <summary>Populated for students only: their own submission for this assignment, if any.</summary>
    public StudentSubmissionSummary? MySubmission { get; init; }
}

public record StudentSubmissionSummary
{
    public Guid Id { get; init; }

    public SubmissionStatus Status { get; init; }

    public string StatusName { get; init; } = string.Empty;

    public DateTime SubmittedAtUtc { get; init; }

    public bool IsLate { get; init; }

    public decimal? Marks { get; init; }

    public string? Feedback { get; init; }
}

public record CreateAssignmentRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MinLength(3, ErrorMessage = "Title must be at least 3 characters.")]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(5000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public Guid ClassRoomId { get; init; }

    [Required]
    public Guid SubjectId { get; init; }

    [Range(1, 1000, ErrorMessage = "Maximum marks must be between 1 and 1000.")]
    public decimal MaxMarks { get; init; }

    [Required(ErrorMessage = "Deadline is required.")]
    public DateTime DeadlineUtc { get; init; }

    public bool AllowLateSubmission { get; init; }

    public bool AllowResubmission { get; init; } = true;

    /// <summary>When true the assignment is created as Published instead of Draft.</summary>
    public bool PublishImmediately { get; init; }
}

public record UpdateAssignmentRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public Guid ClassRoomId { get; init; }

    [Required]
    public Guid SubjectId { get; init; }

    [Range(1, 1000)]
    public decimal MaxMarks { get; init; }

    [Required]
    public DateTime DeadlineUtc { get; init; }

    public bool AllowLateSubmission { get; init; }

    public bool AllowResubmission { get; init; } = true;
}

public class AssignmentQuery : PageRequest
{
    public Guid? ClassRoomId { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? TeacherId { get; set; }

    public AssignmentStatus? Status { get; set; }

    /// <summary>When true only assignments whose deadline has not passed are returned.</summary>
    public bool? UpcomingOnly { get; set; }
}
