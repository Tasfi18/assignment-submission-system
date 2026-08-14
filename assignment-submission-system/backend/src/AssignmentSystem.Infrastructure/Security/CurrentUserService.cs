using System.Security.Claims;
using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AssignmentSystem.Infrastructure.Security;

/// <summary>Reads the authenticated principal that the JWT middleware placed on the request.</summary>
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public UserRole? Role
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(raw, ignoreCase: true, out var role) ? role : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true && UserId.HasValue;

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedException("You must be signed in to perform this action.");
}
