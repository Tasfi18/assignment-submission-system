using AssignmentSystem.Domain.Common;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Domain.Entities;

/// <summary>A student's answer to an assignment. At most one row per (assignment, student).</summary>
public class Submission : BaseEntity
{
    public Guid AssignmentId { get; set; }

    public Assignment Assignment { get; set; } = null!;

    public Guid StudentId { get; set; }

    public User Student { get; set; } = null!;

    /// <summary>The written answer.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Optional link to an uploaded file (Google Drive, S3, ...). File storage is out of scope.</summary>
    public string? AttachmentUrl { get; set; }

    public DateTime SubmittedAtUtc { get; set; }

    public DateTime? LastUpdatedAtUtc { get; set; }

    /// <summary>Computed once at submission/update time against the assignment deadline.</summary>
    public bool IsLate { get; set; }

    public int RevisionCount { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public decimal? Marks { get; set; }

    public string? Feedback { get; set; }

    public Guid? GradedByUserId { get; set; }

    public User? GradedByUser { get; set; }

    public DateTime? GradedAtUtc { get; set; }
}
