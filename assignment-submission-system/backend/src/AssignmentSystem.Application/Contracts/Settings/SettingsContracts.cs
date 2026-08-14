using System.ComponentModel.DataAnnotations;

namespace AssignmentSystem.Application.Contracts.Settings;

public record AppSettingResponse
{
    public string Key { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string? Description { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

public record UpdateSettingRequest
{
    [Required]
    [MaxLength(500)]
    public string Value { get; init; } = string.Empty;
}

public record DashboardResponse
{
    public int TotalUsers { get; init; }

    public int TotalTeachers { get; init; }

    public int TotalStudents { get; init; }

    public int TotalClassRooms { get; init; }

    public int TotalSubjects { get; init; }

    public int TotalAssignments { get; init; }

    public int PublishedAssignments { get; init; }

    public int DraftAssignments { get; init; }

    public int TotalSubmissions { get; init; }

    public int PendingReviewSubmissions { get; init; }

    public int GradedSubmissions { get; init; }

    public int LateSubmissions { get; init; }
}
