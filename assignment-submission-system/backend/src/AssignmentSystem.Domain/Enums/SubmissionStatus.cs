namespace AssignmentSystem.Domain.Enums;

/// <summary>Lifecycle of a student's submission.</summary>
public enum SubmissionStatus
{
    Submitted = 1,
    UnderReview = 2,
    Graded = 3,
    ReturnedForRevision = 4,
    Rejected = 5
}
