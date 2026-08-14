using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Mapping;
using AssignmentSystem.Application.Contracts.Auth;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserSummary> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ICurrentUser _currentUser;
    private readonly ISettingsService _settings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        ICurrentUser currentUser,
        ISettingsService settings,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _currentUser = currentUser;
        _settings = settings;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.ClassRoom)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // The same message is returned for "unknown email" and "wrong password" so the endpoint
        // cannot be used to enumerate accounts.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", email);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated. Please contact the administrator.");
        }

        if (user.Role == UserRole.Student && !await _settings.GetBoolAsync(SettingKeys.StudentPortalEnabled, true, cancellationToken))
        {
            throw new ForbiddenException("The student portal is currently disabled by the administrator.");
        }

        var token = _tokenGenerator.Generate(user);

        _logger.LogInformation("User {UserId} ({Role}) signed in", user.Id, user.Role);

        return new LoginResponse
        {
            AccessToken = token.AccessToken,
            TokenType = "Bearer",
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = user.ToSummary()
        };
    }

    public async Task<UserSummary> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();

        var user = await _db.Users
            .Include(u => u.ClassRoom)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("The signed in account no longer exists.");

        return user.ToSummary();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("The signed in account no longer exists.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationException(nameof(request.CurrentPassword), "The current password is incorrect.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new ValidationException(nameof(request.NewPassword), "The new password must be different from the current one.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} changed their password", user.Id);
    }
}
