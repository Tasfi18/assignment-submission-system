using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Mapping;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Contracts.Assignments;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Services;

public interface IAssignmentService
{
    Task<PagedResult<AssignmentResponse>> GetAsync(AssignmentQuery query, CancellationToken cancellationToken = default);

    Task<AssignmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AssignmentResponse> CreateAsync(CreateAssignmentRequest request, CancellationToken cancellationToken = default);

    Task<AssignmentResponse> UpdateAsync(Guid id, UpdateAssignmentRequest request, CancellationToken cancellationToken = default);

    Task<AssignmentResponse> PublishAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AssignmentResponse> CloseAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Owns every rule about who may see or change an assignment.
///
/// Visibility
///   Admin    - every assignment.
///   Teacher  - the assignments they created.
///   Student  - published/closed assignments targeted at the class they are enrolled in. Drafts are invisible.
///
/// Mutation
///   Only the teacher who owns the assignment may change it. Admins have read-only oversight, which
///   keeps a single, auditable owner for academic content.
/// </summary>
public class AssignmentService : IAssignmentService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ILogger<AssignmentService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PagedResult<AssignmentResponse>> GetAsync(AssignmentQuery query, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();
        var role = _currentUser.Role ?? throw new UnauthorizedException("The access token does not contain a role.");

        var assignments = _db.Assignments
            .Include(a => a.ClassRoom)
            .Include(a => a.Subject)
            .Include(a => a.Teacher)
            .AsNoTracking()
            .AsQueryable();

        Guid? studentClassRoomId = null;

        switch (role)
        {
            case UserRole.Teacher:
                assignments = assignments.Where(a => a.TeacherId == userId);
                break;

            case UserRole.Student:
                studentClassRoomId = await GetStudentClassRoomIdAsync(userId, cancellationToken);
                assignments = assignments.Where(a =>
                    a.ClassRoomId == studentClassRoomId &&
                    (a.Status == AssignmentStatus.Published || a.Status == AssignmentStatus.Closed));
                break;
        }

        if (query.ClassRoomId.HasValue)
        {
            assignments = assignments.Where(a => a.ClassRoomId == query.ClassRoomId.Value);
        }

        if (query.SubjectId.HasValue)
        {
            assignments = assignments.Where(a => a.SubjectId == query.SubjectId.Value);
        }

        if (query.TeacherId.HasValue && role == UserRole.Admin)
        {
            assignments = assignments.Where(a => a.TeacherId == query.TeacherId.Value);
        }

        if (query.Status.HasValue)
        {
            assignments = assignments.Where(a => a.Status == query.Status.Value);
        }

        if (query.UpcomingOnly == true)
        {
            var now = _clock.UtcNow;
            assignments = assignments.Where(a => a.DeadlineUtc >= now);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            assignments = assignments.Where(a => a.Title.ToLower().Contains(term) || a.Description.ToLower().Contains(term));
        }

        var total = await assignments.CountAsync(cancellationToken);

        var page = await assignments
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = await BuildResponsesAsync(page, role, userId, cancellationToken);

        return new PagedResult<AssignmentResponse>(items, total, query.Page, query.PageSize);
    }

    public async Task<AssignmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();
        var role = _currentUser.Role ?? throw new UnauthorizedException("The access token does not contain a role.");

        var assignment = await _db.Assignments
            .Include(a => a.ClassRoom)
            .Include(a => a.Subject)
            .Include(a => a.Teacher)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Assignment", id);

        await EnsureCanReadAsync(assignment, role, userId, cancellationToken);

        var responses = await BuildResponsesAsync(new List<Assignment> { assignment }, role, userId, cancellationToken);

        return responses[0];
    }

    public async Task<AssignmentResponse> CreateAsync(CreateAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        await EnsureTeachesAsync(teacherId, request.ClassRoomId, request.SubjectId, cancellationToken);

        if (request.MaxMarks <= 0)
        {
            throw new ValidationException(nameof(request.MaxMarks), "Maximum marks must be greater than zero.");
        }

        var deadline = NormaliseToUtc(request.DeadlineUtc);

        // A draft may hold a placeholder deadline, but anything students can see must still be open.
        if (request.PublishImmediately && deadline <= _clock.UtcNow)
        {
            throw new BusinessRuleException("The deadline must be in the future to publish an assignment.");
        }

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ClassRoomId = request.ClassRoomId,
            SubjectId = request.SubjectId,
            TeacherId = teacherId,
            MaxMarks = request.MaxMarks,
            DeadlineUtc = deadline,
            AllowLateSubmission = request.AllowLateSubmission,
            AllowResubmission = request.AllowResubmission,
            Status = request.PublishImmediately ? AssignmentStatus.Published : AssignmentStatus.Draft,
            PublishedAtUtc = request.PublishImmediately ? _clock.UtcNow : null,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} created assignment {AssignmentId} ({Status})",
            teacherId, assignment.Id, assignment.Status);

        return await GetByIdAsync(assignment.Id, cancellationToken);
    }

    public async Task<AssignmentResponse> UpdateAsync(Guid id, UpdateAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var assignment = await _db.Assignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Assignment", id);

        EnsureOwner(assignment, teacherId);

        if (assignment.Status == AssignmentStatus.Closed)
        {
            throw new BusinessRuleException("A closed assignment cannot be edited. Re-open it by creating a new assignment.");
        }

        var submissionCount = await _db.Submissions.CountAsync(s => s.AssignmentId == id, cancellationToken);

        // Re-targeting an assignment after students answered it would silently orphan their work.
        if (submissionCount > 0 && (assignment.ClassRoomId != request.ClassRoomId || assignment.SubjectId != request.SubjectId))
        {
            throw new BusinessRuleException("The class or subject cannot be changed once students have submitted.");
        }

        await EnsureTeachesAsync(teacherId, request.ClassRoomId, request.SubjectId, cancellationToken);

        if (request.MaxMarks <= 0)
        {
            throw new ValidationException(nameof(request.MaxMarks), "Maximum marks must be greater than zero.");
        }

        var awardedMarks = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.AssignmentId == id && s.Marks != null)
            .Select(s => s.Marks!.Value)
            .ToListAsync(cancellationToken);

        var highestAwarded = awardedMarks.Count == 0 ? 0m : awardedMarks.Max();

        if (request.MaxMarks < highestAwarded)
        {
            throw new BusinessRuleException(
                $"Maximum marks cannot be lower than {highestAwarded} because marks have already been awarded.");
        }

        var deadline = NormaliseToUtc(request.DeadlineUtc);

        if (assignment.Status == AssignmentStatus.Published && deadline <= _clock.UtcNow)
        {
            throw new BusinessRuleException("The deadline of a published assignment must stay in the future.");
        }

        assignment.Title = request.Title.Trim();
        assignment.Description = request.Description.Trim();
        assignment.ClassRoomId = request.ClassRoomId;
        assignment.SubjectId = request.SubjectId;
        assignment.MaxMarks = request.MaxMarks;
        assignment.DeadlineUtc = deadline;
        assignment.AllowLateSubmission = request.AllowLateSubmission;
        assignment.AllowResubmission = request.AllowResubmission;
        assignment.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assignment {AssignmentId} updated by teacher {TeacherId}", id, teacherId);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<AssignmentResponse> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var assignment = await _db.Assignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Assignment", id);

        EnsureOwner(assignment, teacherId);

        if (assignment.Status != AssignmentStatus.Draft)
        {
            throw new BusinessRuleException($"Only a draft can be published. This assignment is {assignment.Status}.");
        }

        if (assignment.DeadlineUtc <= _clock.UtcNow)
        {
            throw new BusinessRuleException("The deadline must be in the future to publish an assignment.");
        }

        var hasStudents = await _db.Users.AnyAsync(
            u => u.Role == UserRole.Student && u.ClassRoomId == assignment.ClassRoomId, cancellationToken);

        if (!hasStudents)
        {
            throw new BusinessRuleException("There are no students enrolled in the target class yet.");
        }

        assignment.Status = AssignmentStatus.Published;
        assignment.PublishedAtUtc = _clock.UtcNow;
        assignment.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assignment {AssignmentId} published", id);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<AssignmentResponse> CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var assignment = await _db.Assignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Assignment", id);

        EnsureOwner(assignment, teacherId);

        if (assignment.Status != AssignmentStatus.Published)
        {
            throw new BusinessRuleException($"Only a published assignment can be closed. This assignment is {assignment.Status}.");
        }

        assignment.Status = AssignmentStatus.Closed;
        assignment.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assignment {AssignmentId} closed", id);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var assignment = await _db.Assignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Assignment", id);

        EnsureOwner(assignment, teacherId);

        if (await _db.Submissions.AnyAsync(s => s.AssignmentId == id, cancellationToken))
        {
            throw new BusinessRuleException("Students have already submitted, so this assignment cannot be deleted. Close it instead.");
        }

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assignment {AssignmentId} deleted by teacher {TeacherId}", id, teacherId);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<IReadOnlyList<AssignmentResponse>> BuildResponsesAsync(
        IReadOnlyList<Assignment> assignments,
        UserRole role,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (assignments.Count == 0)
        {
            return Array.Empty<AssignmentResponse>();
        }

        var ids = assignments.Select(a => a.Id).ToList();
        var classRoomIds = assignments.Select(a => a.ClassRoomId).Distinct().ToList();

        var submissionCounts = await _db.Submissions
            .AsNoTracking()
            .Where(s => ids.Contains(s.AssignmentId))
            .GroupBy(s => s.AssignmentId)
            .Select(g => new { AssignmentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var classStudentCounts = await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Student && u.ClassRoomId != null && classRoomIds.Contains(u.ClassRoomId.Value))
            .GroupBy(u => u.ClassRoomId!.Value)
            .Select(g => new { ClassRoomId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        List<Submission> mySubmissions = new();

        if (role == UserRole.Student)
        {
            mySubmissions = await _db.Submissions
                .AsNoTracking()
                .Where(s => s.StudentId == userId && ids.Contains(s.AssignmentId))
                .ToListAsync(cancellationToken);
        }

        var now = _clock.UtcNow;

        return assignments.Select(a => a.ToResponse(
            now,
            submissionCounts.FirstOrDefault(c => c.AssignmentId == a.Id)?.Count ?? 0,
            classStudentCounts.FirstOrDefault(c => c.ClassRoomId == a.ClassRoomId)?.Count ?? 0,
            mySubmissions.FirstOrDefault(s => s.AssignmentId == a.Id))).ToList();
    }

    private async Task EnsureCanReadAsync(Assignment assignment, UserRole role, Guid userId, CancellationToken cancellationToken)
    {
        switch (role)
        {
            case UserRole.Admin:
                return;

            case UserRole.Teacher:
                if (assignment.TeacherId != userId)
                {
                    throw new ForbiddenException("This assignment belongs to another teacher.");
                }

                return;

            case UserRole.Student:
                var classRoomId = await GetStudentClassRoomIdAsync(userId, cancellationToken);

                if (assignment.ClassRoomId != classRoomId)
                {
                    throw new ForbiddenException("This assignment was not created for your class.");
                }

                if (assignment.Status == AssignmentStatus.Draft)
                {
                    // Drafts must not leak their existence, so this is a 404 rather than a 403.
                    throw new NotFoundException("Assignment", assignment.Id);
                }

                return;

            default:
                throw new ForbiddenException("You are not allowed to view this assignment.");
        }
    }

    private void EnsureOwner(Assignment assignment, Guid teacherId)
    {
        if (assignment.TeacherId != teacherId)
        {
            throw new ForbiddenException("Only the teacher who created this assignment can modify it.");
        }
    }

    /// <summary>A teacher may only work inside the class/subject pairs an admin gave them.</summary>
    private async Task EnsureTeachesAsync(Guid teacherId, Guid classRoomId, Guid subjectId, CancellationToken cancellationToken)
    {
        var allowed = await _db.ClassSubjectTeachers.AnyAsync(
            t => t.TeacherId == teacherId && t.ClassRoomId == classRoomId && t.SubjectId == subjectId,
            cancellationToken);

        if (!allowed)
        {
            throw new ForbiddenException("You are not assigned to this class and subject.");
        }
    }

    private async Task<Guid> GetStudentClassRoomIdAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var classRoomId = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == studentId)
            .Select(u => u.ClassRoomId)
            .FirstOrDefaultAsync(cancellationToken);

        if (classRoomId is null || classRoomId == Guid.Empty)
        {
            throw new BusinessRuleException("You are not enrolled in any class yet. Please contact the administrator.");
        }

        return classRoomId.Value;
    }

    /// <summary>
    /// Clients may send a local or unspecified <see cref="DateTime"/>; PostgreSQL "timestamp with time zone"
    /// requires UTC, so every incoming deadline is normalised here.
    /// </summary>
    private static DateTime NormaliseToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
