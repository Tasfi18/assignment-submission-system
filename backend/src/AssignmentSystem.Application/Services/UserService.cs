using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Mapping;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Contracts.Users;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Services;

public interface IUserService
{
    Task<PagedResult<UserResponse>> GetAsync(UserQuery query, CancellationToken cancellationToken = default);

    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse> SetActiveStateAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Admin-only user administration. Every method assumes the controller already enforced the Admin role.</summary>
public class UserService : IUserService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ILogger<UserService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PagedResult<UserResponse>> GetAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var users = _db.Users.Include(u => u.ClassRoom).AsNoTracking().AsQueryable();

        if (query.Role.HasValue)
        {
            users = users.Where(u => u.Role == query.Role.Value);
        }

        if (query.ClassRoomId.HasValue)
        {
            users = users.Where(u => u.ClassRoomId == query.ClassRoomId.Value);
        }

        if (query.IsActive.HasValue)
        {
            users = users.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            users = users.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        var total = await users.CountAsync(cancellationToken);

        var page = await users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserResponse>(page.Select(u => u.ToResponse()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.ClassRoom)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        return user.ToResponse();
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = Normalise(request.Email);

        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException($"An account already exists for '{email}'.");
        }

        var classRoomId = await ResolveClassRoomAsync(request.Role, request.ClassRoomId, cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true,
            ClassRoomId = classRoomId,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Role} account {UserId}", user.Role, user.Id);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        var email = Normalise(request.Email);

        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id, cancellationToken))
        {
            throw new ConflictException($"An account already exists for '{email}'.");
        }

        if (!request.IsActive && user.Id == _currentUser.UserId)
        {
            throw new BusinessRuleException("You cannot deactivate your own account.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.IsActive = request.IsActive;
        user.ClassRoomId = await ResolveClassRoomAsync(user.Role, request.ClassRoomId, cancellationToken);
        user.UpdatedAtUtc = _clock.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            _logger.LogInformation("Password reset by admin for user {UserId}", user.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> SetActiveStateAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        if (!isActive && user.Id == _currentUser.UserId)
        {
            throw new BusinessRuleException("You cannot deactivate your own account.");
        }

        user.IsActive = isActive;
        user.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        if (user.Id == _currentUser.UserId)
        {
            throw new BusinessRuleException("You cannot delete your own account.");
        }

        // Academic history must survive: refuse a hard delete once the account produced data and
        // point the admin at deactivation instead.
        var hasAssignments = await _db.Assignments.AnyAsync(a => a.TeacherId == id, cancellationToken);
        var hasSubmissions = await _db.Submissions.AnyAsync(s => s.StudentId == id, cancellationToken);

        if (hasAssignments || hasSubmissions)
        {
            throw new BusinessRuleException(
                "This account has assignments or submissions attached to it and cannot be deleted. Deactivate it instead.");
        }

        var teachingLinks = await _db.ClassSubjectTeachers.Where(t => t.TeacherId == id).ToListAsync(cancellationToken);
        if (teachingLinks.Count > 0)
        {
            _db.ClassSubjectTeachers.RemoveRange(teachingLinks);
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted user {UserId}", id);
    }

    /// <summary>Students must belong to a class; teachers and admins must not.</summary>
    private async Task<Guid?> ResolveClassRoomAsync(UserRole role, Guid? classRoomId, CancellationToken cancellationToken)
    {
        if (role != UserRole.Student)
        {
            return null;
        }

        if (!classRoomId.HasValue || classRoomId.Value == Guid.Empty)
        {
            throw new ValidationException(nameof(CreateUserRequest.ClassRoomId), "A student must be assigned to a class.");
        }

        var exists = await _db.ClassRooms.AnyAsync(c => c.Id == classRoomId.Value, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("ClassRoom", classRoomId.Value);
        }

        return classRoomId;
    }

    private static string Normalise(string email) => email.Trim().ToLowerInvariant();
}
