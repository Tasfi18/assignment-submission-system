using System.Globalization;
using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Mapping;
using AssignmentSystem.Application.Contracts.Settings;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Services;

public interface ISettingsService
{
    Task<IReadOnlyList<AppSettingResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AppSettingResponse> UpdateAsync(string key, UpdateSettingRequest request, CancellationToken cancellationToken = default);

    Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken cancellationToken = default);

    Task<int> GetIntAsync(string key, int fallback, CancellationToken cancellationToken = default);

    Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public class SettingsService : ISettingsService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(IApplicationDbContext db, IDateTimeProvider clock, ILogger<SettingsService> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AppSettingResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.AppSettings
            .AsNoTracking()
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);

        return settings.Select(s => s.ToResponse()).ToList();
    }

    public async Task<AppSettingResponse> UpdateAsync(string key, UpdateSettingRequest request, CancellationToken cancellationToken = default)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
            ?? throw new NotFoundException($"Setting '{key}' was not found.");

        setting.Value = request.Value.Trim();
        setting.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Setting {Key} updated to {Value}", key, setting.Value);

        return setting.ToResponse();
    }

    /// <summary>Reads a boolean setting, falling back to <paramref name="fallback"/> when it is missing or unparsable.</summary>
    public async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken cancellationToken = default)
    {
        var raw = await ReadRawAsync(key, cancellationToken);
        return bool.TryParse(raw, out var parsed) ? parsed : fallback;
    }

    public async Task<int> GetIntAsync(string key, int fallback, CancellationToken cancellationToken = default)
    {
        var raw = await ReadRawAsync(key, cancellationToken);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users.AsNoTracking()
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var assignments = await _db.Assignments.AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var submissions = await _db.Submissions.AsNoTracking()
            .Select(s => new { s.Status, s.IsLate })
            .ToListAsync(cancellationToken);

        return new DashboardResponse
        {
            TotalUsers = users.Sum(u => u.Count),
            TotalTeachers = users.FirstOrDefault(u => u.Role == UserRole.Teacher)?.Count ?? 0,
            TotalStudents = users.FirstOrDefault(u => u.Role == UserRole.Student)?.Count ?? 0,
            TotalClassRooms = await _db.ClassRooms.CountAsync(cancellationToken),
            TotalSubjects = await _db.Subjects.CountAsync(cancellationToken),
            TotalAssignments = assignments.Sum(a => a.Count),
            PublishedAssignments = assignments.FirstOrDefault(a => a.Status == AssignmentStatus.Published)?.Count ?? 0,
            DraftAssignments = assignments.FirstOrDefault(a => a.Status == AssignmentStatus.Draft)?.Count ?? 0,
            TotalSubmissions = submissions.Count,
            PendingReviewSubmissions = submissions.Count(s => s.Status == SubmissionStatus.Submitted || s.Status == SubmissionStatus.UnderReview),
            GradedSubmissions = submissions.Count(s => s.Status == SubmissionStatus.Graded),
            LateSubmissions = submissions.Count(s => s.IsLate)
        };
    }

    private async Task<string?> ReadRawAsync(string key, CancellationToken cancellationToken)
    {
        return await _db.AppSettings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
