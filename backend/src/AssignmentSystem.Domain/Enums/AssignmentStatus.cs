namespace AssignmentSystem.Domain.Enums;

/// <summary>Lifecycle of an assignment created by a teacher.</summary>
public enum AssignmentStatus
{
    /// <summary>Visible only to the owning teacher and admins. Students cannot see or submit.</summary>
    Draft = 1,

    /// <summary>Visible to the students of the target class. Submissions are accepted.</summary>
    Published = 2,

    /// <summary>Visible to students, but no new submissions or updates are accepted.</summary>
    Closed = 3
}
