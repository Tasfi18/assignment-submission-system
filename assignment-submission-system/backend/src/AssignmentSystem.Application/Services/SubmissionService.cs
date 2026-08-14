using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Mapping;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Contracts.Submissions;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Services;

public interface ISubmissionService
{
    Task<PagedResult<SubmissionResponse>> GetAsync(SubmissionQuery query, CancellationToken cancellationToken = default);

    Task<SubmissionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SubmissionResponse> SubmitAsync(Guid assignmentId, CreateSubmissionRequest request, CancellationToken cancellationToken = default);

    Task<SubmissionResponse> UpdateAsync(Guid submissionId, UpdateSubmissionRequest request, CancellationToken cancellationToken = default);

    Task<SubmissionResponse> GradeAsync(Guid submissionId, GradeSubmissionRequest request, CancellationToken cancellationToken = default);

    Task<SubmissionResponse> UpdateStatusAsync(Guid submissionId, UpdateSubmissionStatusRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Owns the submission workflow.
///
/// Submitting
///   * only students, only for a Published assignment targeted at their own class;
///   * exactly one submission per (assignment, student) - a second attempt must go through update;
///   * after the deadline a submission is only accepted when the assignment allows late work AND the
///     global late-submission switch is on; such submissions are flagged IsLate.
///
/// Updating
///   * only the owning student, only while the assignment allows resubmission;
///   * never after the submission has been graded;
///   * the same deadline/late rules as the first submission.
///
/// Grading
///   * only the teacher who owns the assignment;
///   * marks must fall between 0 and the assignment's maximum.
/// </summary>
public class SubmissionService : ISubmissionService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISettingsService _settings;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ISettingsService settings,
        ILogger<SubmissionService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _settings = settings;
        _logger = logger;
    }

    public async Task<PagedResult<SubmissionResponse>> GetAsync(SubmissionQuery query, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();
        var role = _currentUser.Role ?? throw new UnauthorizedException("The access token does not contain a role.");

        var submissions = BaseQuery().AsQueryable();

        switch (role)
        {
            case UserRole.Teacher:
                submissions = submissions.Where(s => s.Assignment.TeacherId == userId);
                break;

            case UserRole.Student:
                submissions = submissions.Where(s => s.StudentId == userId);
                break;
        }

        if (query.AssignmentId.HasValue)
        {
            submissions = submissions.Where(s => s.AssignmentId == query.AssignmentId.Value);
        }

        if (query.StudentId.HasValue && role != UserRole.Student)
        {
            submissions = submissions.Where(s => s.StudentId == query.StudentId.Value);
        }

        if (query.ClassRoomId.HasValue)
        {
            submissions = submissions.Where(s => s.Assignment.ClassRoomId == query.ClassRoomId.Value);
        }

        if (query.Status.HasValue)
        {
            submissions = submissions.Where(s => s.Status == query.Status.Value);
        }

        if (query.IsLate.HasValue)
        {
            submissions = submissions.Where(s => s.IsLate == query.IsLate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            submissions = submissions.Where(s =>
                s.Student.FullName.ToLower().Contains(term) ||
                s.Assignment.Title.ToLower().Contains(term));
        }

        var total = await submissions.CountAsync(cancellationToken);

        var page = await submissions
            .OrderByDescending(s => s.SubmittedAtUtc)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SubmissionResponse>(
            page.Select(s => s.ToResponse()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<SubmissionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();
        var role = _currentUser.Role ?? throw new UnauthorizedException("The access token does not contain a role.");

        var submission = await BaseQuery().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Submission", id);

        EnsureCanRead(submission, role, userId);

        return submission.ToResponse();
    }

    public async Task<SubmissionResponse> SubmitAsync(Guid assignmentId, CreateSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var studentId = _currentUser.RequireUserId();

        var student = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("The signed in account no longer exists.");

        var assignment = await _db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken)
            ?? throw new NotFoundException("Assignment", assignmentId);

        if (student.ClassRoomId is null || assignment.ClassRoomId != student.ClassRoomId)
        {
            throw new ForbiddenException("This assignment was not created for your class.");
        }

        if (assignment.Status == AssignmentStatus.Draft)
        {
            // Do not confirm that an unpublished assignment exists.
            throw new NotFoundException("Assignment", assignmentId);
        }

        if (assignment.Status == AssignmentStatus.Closed)
        {
            throw new BusinessRuleException("This assignment is closed and no longer accepts submissions.");
        }

        var duplicate = await _db.Submissions.AnyAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == studentId, cancellationToken);

        if (duplicate)
        {
            throw new ConflictException("You have already submitted this assignment. Update your existing submission instead.");
        }

        await ValidateContentLengthAsync(request.Content, cancellationToken);

        var isLate = await EvaluateDeadlineAsync(assignment, cancellationToken);

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignmentId,
            StudentId = studentId,
            Content = request.Content.Trim(),
            AttachmentUrl = string.IsNullOrWhiteSpace(request.AttachmentUrl) ? null : request.AttachmentUrl.Trim(),
            SubmittedAtUtc = _clock.UtcNow,
            IsLate = isLate,
            RevisionCount = 0,
            Status = SubmissionStatus.Submitted,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Student {StudentId} submitted assignment {AssignmentId} (late: {IsLate})",
            studentId, assignmentId, isLate);

        return await LoadResponseAsync(submission.Id, cancellationToken);
    }

    public async Task<SubmissionResponse> UpdateAsync(Guid submissionId, UpdateSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var studentId = _currentUser.RequireUserId();

        var submission = await _db.Submissions
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new NotFoundException("Submission", submissionId);

        if (submission.StudentId != studentId)
        {
            throw new ForbiddenException("You can only update your own submission.");
        }

        var assignment = submission.Assignment;

        if (assignment.Status == AssignmentStatus.Closed)
        {
            throw new BusinessRuleException("This assignment is closed and no longer accepts changes.");
        }

        if (submission.Status == SubmissionStatus.Graded)
        {
            throw new BusinessRuleException("This submission has already been graded and can no longer be changed.");
        }

        if (submission.Status == SubmissionStatus.Rejected)
        {
            throw new BusinessRuleException("This submission was rejected by the teacher. Please contact them directly.");
        }

        // A teacher who explicitly asked for a revision always re-opens the submission, even when
        // the assignment normally forbids resubmission.
        if (!assignment.AllowResubmission && submission.Status != SubmissionStatus.ReturnedForRevision)
        {
            throw new BusinessRuleException("This assignment does not allow a submission to be updated.");
        }

        await ValidateContentLengthAsync(request.Content, cancellationToken);

        var isLate = await EvaluateDeadlineAsync(assignment, cancellationToken);

        submission.Content = request.Content.Trim();
        submission.AttachmentUrl = string.IsNullOrWhiteSpace(request.AttachmentUrl) ? null : request.AttachmentUrl.Trim();
        submission.LastUpdatedAtUtc = _clock.UtcNow;
        submission.UpdatedAtUtc = _clock.UtcNow;
        submission.RevisionCount += 1;
        submission.IsLate = isLate;
        submission.Status = SubmissionStatus.Submitted;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Submission {SubmissionId} updated (revision {Revision})", submissionId, submission.RevisionCount);

        return await LoadResponseAsync(submissionId, cancellationToken);
    }

    public async Task<SubmissionResponse> GradeAsync(Guid submissionId, GradeSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var submission = await _db.Submissions
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new NotFoundException("Submission", submissionId);

        EnsureOwnsAssignment(submission, teacherId);

        if (request.Marks < 0)
        {
            throw new ValidationException(nameof(request.Marks), "Marks cannot be negative.");
        }

        if (request.Marks > submission.Assignment.MaxMarks)
        {
            throw new ValidationException(
                nameof(request.Marks),
                $"Marks cannot exceed the maximum of {submission.Assignment.MaxMarks} for this assignment.");
        }

        submission.Marks = request.Marks;
        submission.Feedback = string.IsNullOrWhiteSpace(request.Feedback) ? null : request.Feedback.Trim();
        submission.Status = SubmissionStatus.Graded;
        submission.GradedByUserId = teacherId;
        submission.GradedAtUtc = _clock.UtcNow;
        submission.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} graded submission {SubmissionId} with {Marks}/{MaxMarks}",
            teacherId, submissionId, request.Marks, submission.Assignment.MaxMarks);

        return await LoadResponseAsync(submissionId, cancellationToken);
    }

    public async Task<SubmissionResponse> UpdateStatusAsync(Guid submissionId, UpdateSubmissionStatusRequest request, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var submission = await _db.Submissions
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new NotFoundException("Submission", submissionId);

        EnsureOwnsAssignment(submission, teacherId);

        if (request.Status == SubmissionStatus.Graded && submission.Marks is null)
        {
            throw new BusinessRuleException("Award marks before moving a submission to the Graded state.");
        }

        submission.Status = request.Status;
        submission.UpdatedAtUtc = _clock.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Feedback))
        {
            submission.Feedback = request.Feedback.Trim();
        }

        if (request.Status == SubmissionStatus.ReturnedForRevision)
        {
            // The student is being asked to try again, so any provisional marks are cleared.
            submission.Marks = null;
            submission.GradedAtUtc = null;
            submission.GradedByUserId = null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Submission {SubmissionId} moved to {Status}", submissionId, request.Status);

        return await LoadResponseAsync(submissionId, cancellationToken);
    }

    // ------------------------------------------------------------------ helpers

    private IQueryable<Submission> BaseQuery() =>
        _db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Subject)
            .Include(s => s.Assignment).ThenInclude(a => a.ClassRoom)
            .Include(s => s.Student)
            .Include(s => s.GradedByUser)
            .AsNoTracking();

    private async Task<SubmissionResponse> LoadResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        var saved = await BaseQuery().FirstAsync(s => s.Id == id, cancellationToken);
        return saved.ToResponse();
    }

    private static void EnsureCanRead(Submission submission, UserRole role, Guid userId)
    {
        switch (role)
        {
            case UserRole.Admin:
                return;

            case UserRole.Teacher when submission.Assignment.TeacherId == userId:
                return;

            case UserRole.Student when submission.StudentId == userId:
                return;

            default:
                throw new ForbiddenException("You are not allowed to view this submission.");
        }
    }

    private static void EnsureOwnsAssignment(Submission submission, Guid teacherId)
    {
        if (submission.Assignment.TeacherId != teacherId)
        {
            throw new ForbiddenException("Only the teacher who owns the assignment can review this submission.");
        }
    }

    /// <summary>
    /// Returns true when the submission is late. Throws when late work is not accepted.
    /// </summary>
    private async Task<bool> EvaluateDeadlineAsync(Assignment assignment, CancellationToken cancellationToken)
    {
        if (_clock.UtcNow <= assignment.DeadlineUtc)
        {
            return false;
        }

        var lateAllowedGlobally = await _settings.GetBoolAsync(
            SettingKeys.AllowLateSubmissionsGlobally, true, cancellationToken);

        if (!assignment.AllowLateSubmission || !lateAllowedGlobally)
        {
            throw new BusinessRuleException("The deadline for this assignment has passed.");
        }

        return true;
    }

    private async Task ValidateContentLengthAsync(string content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationException(nameof(CreateSubmissionRequest.Content), "An answer is required.");
        }

        var maxLength = await _settings.GetIntAsync(SettingKeys.MaxSubmissionLength, 20000, cancellationToken);

        if (content.Trim().Length > maxLength)
        {
            throw new ValidationException(
                nameof(CreateSubmissionRequest.Content),
                $"The answer cannot be longer than {maxLength} characters.");
        }
    }
}
