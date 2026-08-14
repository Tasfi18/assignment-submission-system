using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Mapping;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Contracts.Academics;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Services;

public interface IAcademicService
{
    Task<PagedResult<ClassRoomResponse>> GetClassRoomsAsync(PageRequest query, CancellationToken cancellationToken = default);

    Task<ClassRoomResponse> GetClassRoomAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ClassRoomResponse> CreateClassRoomAsync(CreateClassRoomRequest request, CancellationToken cancellationToken = default);

    Task<ClassRoomResponse> UpdateClassRoomAsync(Guid id, UpdateClassRoomRequest request, CancellationToken cancellationToken = default);

    Task DeleteClassRoomAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<SubjectResponse>> GetSubjectsAsync(PageRequest query, CancellationToken cancellationToken = default);

    Task<SubjectResponse> GetSubjectAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken cancellationToken = default);

    Task<SubjectResponse> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request, CancellationToken cancellationToken = default);

    Task DeleteSubjectAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<TeachingAssignmentResponse>> GetTeachingAssignmentsAsync(TeachingAssignmentQuery query, CancellationToken cancellationToken = default);

    Task<TeachingAssignmentResponse> CreateTeachingAssignmentAsync(CreateTeachingAssignmentRequest request, CancellationToken cancellationToken = default);

    Task DeleteTeachingAssignmentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The class/subject pairs the signed in teacher is allowed to work with.</summary>
    Task<IReadOnlyList<TeachingAssignmentResponse>> GetMyTeachingAssignmentsAsync(CancellationToken cancellationToken = default);
}

public class AcademicService : IAcademicService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AcademicService> _logger;

    public AcademicService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ILogger<AcademicService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    // ---------------------------------------------------------------- classes

    public async Task<PagedResult<ClassRoomResponse>> GetClassRoomsAsync(PageRequest query, CancellationToken cancellationToken = default)
    {
        var classes = _db.ClassRooms.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            classes = classes.Where(c => c.Name.ToLower().Contains(term) || c.Code.ToLower().Contains(term));
        }

        var total = await classes.CountAsync(cancellationToken);

        var page = await classes
            .OrderByDescending(c => c.AcademicYear)
            .ThenBy(c => c.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var pageIds = page.Select(c => c.Id).ToList();

        var studentCounts = await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Student && u.ClassRoomId != null && pageIds.Contains(u.ClassRoomId.Value))
            .GroupBy(u => u.ClassRoomId!.Value)
            .Select(g => new { ClassRoomId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var items = page
            .Select(c => c.ToResponse(studentCounts.FirstOrDefault(x => x.ClassRoomId == c.Id)?.Count ?? 0))
            .ToList();

        return new PagedResult<ClassRoomResponse>(items, total, query.Page, query.PageSize);
    }

    public async Task<ClassRoomResponse> GetClassRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var classRoom = await _db.ClassRooms.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("ClassRoom", id);

        var studentCount = await _db.Users.CountAsync(u => u.ClassRoomId == id && u.Role == UserRole.Student, cancellationToken);

        return classRoom.ToResponse(studentCount);
    }

    public async Task<ClassRoomResponse> CreateClassRoomAsync(CreateClassRoomRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _db.ClassRooms.AnyAsync(c => c.Code == code, cancellationToken))
        {
            throw new ConflictException($"A class with code '{code}' already exists.");
        }

        var classRoom = new ClassRoom
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = code,
            AcademicYear = request.AcademicYear,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.ClassRooms.Add(classRoom);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created class {ClassRoomId} ({Code})", classRoom.Id, classRoom.Code);

        return classRoom.ToResponse(0);
    }

    public async Task<ClassRoomResponse> UpdateClassRoomAsync(Guid id, UpdateClassRoomRequest request, CancellationToken cancellationToken = default)
    {
        var classRoom = await _db.ClassRooms.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("ClassRoom", id);

        var code = request.Code.Trim().ToUpperInvariant();

        if (await _db.ClassRooms.AnyAsync(c => c.Code == code && c.Id != id, cancellationToken))
        {
            throw new ConflictException($"A class with code '{code}' already exists.");
        }

        classRoom.Name = request.Name.Trim();
        classRoom.Code = code;
        classRoom.AcademicYear = request.AcademicYear;
        classRoom.Description = request.Description?.Trim();
        classRoom.IsActive = request.IsActive;
        classRoom.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var studentCount = await _db.Users.CountAsync(u => u.ClassRoomId == id && u.Role == UserRole.Student, cancellationToken);

        return classRoom.ToResponse(studentCount);
    }

    public async Task DeleteClassRoomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var classRoom = await _db.ClassRooms.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("ClassRoom", id);

        if (await _db.Users.AnyAsync(u => u.ClassRoomId == id, cancellationToken))
        {
            throw new BusinessRuleException("Students are still enrolled in this class. Move them first or deactivate the class.");
        }

        if (await _db.Assignments.AnyAsync(a => a.ClassRoomId == id, cancellationToken))
        {
            throw new BusinessRuleException("Assignments exist for this class. Deactivate the class instead of deleting it.");
        }

        var links = await _db.ClassSubjectTeachers.Where(t => t.ClassRoomId == id).ToListAsync(cancellationToken);
        _db.ClassSubjectTeachers.RemoveRange(links);
        _db.ClassRooms.Remove(classRoom);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted class {ClassRoomId}", id);
    }

    // --------------------------------------------------------------- subjects

    public async Task<PagedResult<SubjectResponse>> GetSubjectsAsync(PageRequest query, CancellationToken cancellationToken = default)
    {
        var subjects = _db.Subjects.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            subjects = subjects.Where(s => s.Name.ToLower().Contains(term) || s.Code.ToLower().Contains(term));
        }

        var total = await subjects.CountAsync(cancellationToken);

        var page = await subjects
            .OrderBy(s => s.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SubjectResponse>(page.Select(s => s.ToResponse()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<SubjectResponse> GetSubjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subject = await _db.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Subject", id);

        return subject.ToResponse();
    }

    public async Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _db.Subjects.AnyAsync(s => s.Code == code, cancellationToken))
        {
            throw new ConflictException($"A subject with code '{code}' already exists.");
        }

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(cancellationToken);

        return subject.ToResponse();
    }

    public async Task<SubjectResponse> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request, CancellationToken cancellationToken = default)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Subject", id);

        var code = request.Code.Trim().ToUpperInvariant();

        if (await _db.Subjects.AnyAsync(s => s.Code == code && s.Id != id, cancellationToken))
        {
            throw new ConflictException($"A subject with code '{code}' already exists.");
        }

        subject.Name = request.Name.Trim();
        subject.Code = code;
        subject.Description = request.Description?.Trim();
        subject.IsActive = request.IsActive;
        subject.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return subject.ToResponse();
    }

    public async Task DeleteSubjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Subject", id);

        if (await _db.Assignments.AnyAsync(a => a.SubjectId == id, cancellationToken))
        {
            throw new BusinessRuleException("Assignments exist for this subject. Deactivate the subject instead of deleting it.");
        }

        var links = await _db.ClassSubjectTeachers.Where(t => t.SubjectId == id).ToListAsync(cancellationToken);
        _db.ClassSubjectTeachers.RemoveRange(links);
        _db.Subjects.Remove(subject);

        await _db.SaveChangesAsync(cancellationToken);
    }

    // ---------------------------------------------------- teaching assignments

    public async Task<PagedResult<TeachingAssignmentResponse>> GetTeachingAssignmentsAsync(TeachingAssignmentQuery query, CancellationToken cancellationToken = default)
    {
        var links = _db.ClassSubjectTeachers
            .Include(t => t.ClassRoom)
            .Include(t => t.Subject)
            .Include(t => t.Teacher)
            .AsNoTracking()
            .AsQueryable();

        if (query.ClassRoomId.HasValue)
        {
            links = links.Where(t => t.ClassRoomId == query.ClassRoomId.Value);
        }

        if (query.SubjectId.HasValue)
        {
            links = links.Where(t => t.SubjectId == query.SubjectId.Value);
        }

        if (query.TeacherId.HasValue)
        {
            links = links.Where(t => t.TeacherId == query.TeacherId.Value);
        }

        var total = await links.CountAsync(cancellationToken);

        var page = await links
            .OrderBy(t => t.ClassRoom.Name)
            .ThenBy(t => t.Subject.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TeachingAssignmentResponse>(
            page.Select(t => t.ToResponse()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<TeachingAssignmentResponse> CreateTeachingAssignmentAsync(CreateTeachingAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.TeacherId, cancellationToken)
            ?? throw new NotFoundException("Teacher", request.TeacherId);

        if (teacher.Role != UserRole.Teacher)
        {
            throw new ValidationException(nameof(request.TeacherId), "The selected user is not a teacher.");
        }

        if (!await _db.ClassRooms.AnyAsync(c => c.Id == request.ClassRoomId, cancellationToken))
        {
            throw new NotFoundException("ClassRoom", request.ClassRoomId);
        }

        if (!await _db.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken))
        {
            throw new NotFoundException("Subject", request.SubjectId);
        }

        var duplicate = await _db.ClassSubjectTeachers.AnyAsync(
            t => t.ClassRoomId == request.ClassRoomId && t.SubjectId == request.SubjectId && t.TeacherId == request.TeacherId,
            cancellationToken);

        if (duplicate)
        {
            throw new ConflictException("This teacher is already assigned to that class and subject.");
        }

        var link = new ClassSubjectTeacher
        {
            Id = Guid.NewGuid(),
            ClassRoomId = request.ClassRoomId,
            SubjectId = request.SubjectId,
            TeacherId = request.TeacherId,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.ClassSubjectTeachers.Add(link);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Assigned teacher {TeacherId} to class {ClassRoomId} / subject {SubjectId}",
            request.TeacherId, request.ClassRoomId, request.SubjectId);

        var saved = await _db.ClassSubjectTeachers
            .Include(t => t.ClassRoom)
            .Include(t => t.Subject)
            .Include(t => t.Teacher)
            .AsNoTracking()
            .FirstAsync(t => t.Id == link.Id, cancellationToken);

        return saved.ToResponse();
    }

    public async Task DeleteTeachingAssignmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var link = await _db.ClassSubjectTeachers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Teaching assignment", id);

        var hasAssignments = await _db.Assignments.AnyAsync(
            a => a.TeacherId == link.TeacherId && a.ClassRoomId == link.ClassRoomId && a.SubjectId == link.SubjectId,
            cancellationToken);

        if (hasAssignments)
        {
            throw new BusinessRuleException(
                "The teacher already created assignments for this class and subject, so the mapping cannot be removed.");
        }

        _db.ClassSubjectTeachers.Remove(link);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeachingAssignmentResponse>> GetMyTeachingAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var links = await _db.ClassSubjectTeachers
            .Include(t => t.ClassRoom)
            .Include(t => t.Subject)
            .Include(t => t.Teacher)
            .AsNoTracking()
            .Where(t => t.TeacherId == teacherId)
            .OrderBy(t => t.ClassRoom.Name)
            .ThenBy(t => t.Subject.Name)
            .ToListAsync(cancellationToken);

        return links.Select(t => t.ToResponse()).ToList();
    }
}
