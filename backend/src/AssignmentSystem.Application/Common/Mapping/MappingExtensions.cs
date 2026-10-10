using AssignmentSystem.Application.Contracts.Academics;
using AssignmentSystem.Application.Contracts.Assignments;
using AssignmentSystem.Application.Contracts.Auth;
using AssignmentSystem.Application.Contracts.Settings;
using AssignmentSystem.Application.Contracts.Submissions;
using AssignmentSystem.Application.Contracts.Users;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Common.Mapping;

/// <summary>
/// Hand written entity -&gt; DTO mapping. A mapping library was deliberately avoided: the number of
/// shapes is small and explicit mapping keeps the response payloads obvious during review.
/// </summary>
public static class MappingExtensions
{
    public static UserSummary ToSummary(this User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        RoleName = user.Role.ToString(),
        ClassRoomId = user.ClassRoomId,
        ClassRoomName = user.ClassRoom?.Name
    };

    public static UserResponse ToResponse(this User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        RoleName = user.Role.ToString(),
        IsActive = user.IsActive,
        ClassRoomId = user.ClassRoomId,
        ClassRoomName = user.ClassRoom?.Name,
        CreatedAtUtc = user.CreatedAtUtc
    };

    public static ClassRoomResponse ToResponse(this ClassRoom classRoom, int studentCount) => new()
    {
        Id = classRoom.Id,
        Name = classRoom.Name,
        Code = classRoom.Code,
        AcademicYear = classRoom.AcademicYear,
        Description = classRoom.Description,
        IsActive = classRoom.IsActive,
        StudentCount = studentCount
    };

    public static SubjectResponse ToResponse(this Subject subject) => new()
    {
        Id = subject.Id,
        Name = subject.Name,
        Code = subject.Code,
        Description = subject.Description,
        IsActive = subject.IsActive
    };

    public static TeachingAssignmentResponse ToResponse(this ClassSubjectTeacher link) => new()
    {
        Id = link.Id,
        ClassRoomId = link.ClassRoomId,
        ClassRoomName = link.ClassRoom?.Name ?? string.Empty,
        SubjectId = link.SubjectId,
        SubjectName = link.Subject?.Name ?? string.Empty,
        TeacherId = link.TeacherId,
        TeacherName = link.Teacher?.FullName ?? string.Empty,
        TeacherEmail = link.Teacher?.Email ?? string.Empty,
        CreatedAtUtc = link.CreatedAtUtc
    };

    public static AssignmentResponse ToResponse(
        this Assignment assignment,
        DateTime utcNow,
        int submissionCount = 0,
        int classStudentCount = 0,
        Submission? mySubmission = null) => new()
    {
        Id = assignment.Id,
        Title = assignment.Title,
        Description = assignment.Description,
        ClassRoomId = assignment.ClassRoomId,
        ClassRoomName = assignment.ClassRoom?.Name ?? string.Empty,
        SubjectId = assignment.SubjectId,
        SubjectName = assignment.Subject?.Name ?? string.Empty,
        TeacherId = assignment.TeacherId,
        TeacherName = assignment.Teacher?.FullName ?? string.Empty,
        MaxMarks = assignment.MaxMarks,
        DeadlineUtc = assignment.DeadlineUtc,
        Status = assignment.Status,
        StatusName = assignment.Status.ToString(),
        AllowLateSubmission = assignment.AllowLateSubmission,
        AllowResubmission = assignment.AllowResubmission,
        PublishedAtUtc = assignment.PublishedAtUtc,
        CreatedAtUtc = assignment.CreatedAtUtc,
        IsPastDeadline = assignment.DeadlineUtc < utcNow,
        SubmissionCount = submissionCount,
        ClassStudentCount = classStudentCount,
        MySubmission = mySubmission is null
            ? null
            : new StudentSubmissionSummary
            {
                Id = mySubmission.Id,
                Status = mySubmission.Status,
                StatusName = mySubmission.Status.ToString(),
                SubmittedAtUtc = mySubmission.SubmittedAtUtc,
                IsLate = mySubmission.IsLate,
                Marks = mySubmission.Marks,
                Feedback = mySubmission.Feedback
            }
    };

    public static SubmissionResponse ToResponse(this Submission submission) => new()
    {
        Id = submission.Id,
        AssignmentId = submission.AssignmentId,
        AssignmentTitle = submission.Assignment?.Title ?? string.Empty,
        SubjectName = submission.Assignment?.Subject?.Name ?? string.Empty,
        ClassRoomName = submission.Assignment?.ClassRoom?.Name ?? string.Empty,
        AssignmentMaxMarks = submission.Assignment?.MaxMarks ?? 0m,
        AssignmentDeadlineUtc = submission.Assignment?.DeadlineUtc ?? default,
        StudentId = submission.StudentId,
        StudentName = submission.Student?.FullName ?? string.Empty,
        StudentEmail = submission.Student?.Email ?? string.Empty,
        Content = submission.Content,
        AttachmentUrl = submission.AttachmentUrl,
        SubmittedAtUtc = submission.SubmittedAtUtc,
        LastUpdatedAtUtc = submission.LastUpdatedAtUtc,
        IsLate = submission.IsLate,
        RevisionCount = submission.RevisionCount,
        Status = submission.Status,
        StatusName = submission.Status.ToString(),
        Marks = submission.Marks,
        Feedback = submission.Feedback,
        GradedByName = submission.GradedByUser?.FullName,
        GradedAtUtc = submission.GradedAtUtc
    };

    public static AppSettingResponse ToResponse(this AppSetting setting) => new()
    {
        Key = setting.Key,
        Value = setting.Value,
        Description = setting.Description,
        UpdatedAtUtc = setting.UpdatedAtUtc
    };

    public static string ToDisplayName(this UserRole role) => role.ToString();
}
