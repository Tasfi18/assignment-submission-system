using AssignmentSystem.Domain.Common;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Domain.Entities;

/// <summary>An assignment created by a teacher for one class + subject pair.</summary>
public class Assignment : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid ClassRoomId { get; set; }

    public ClassRoom ClassRoom { get; set; } = null!;

    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    /// <summary>The teacher who owns the assignment.</summary>
    public Guid TeacherId { get; set; }

    public User Teacher { get; set; } = null!;

    public decimal MaxMarks { get; set; }

    public DateTime DeadlineUtc { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;

    /// <summary>When true, students may still submit after the deadline; such submissions are flagged late.</summary>
    public bool AllowLateSubmission { get; set; }

    /// <summary>When true, a student may replace an existing submission (until it is graded / the deadline passes).</summary>
    public bool AllowResubmission { get; set; } = true;

    public DateTime? PublishedAtUtc { get; set; }

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
