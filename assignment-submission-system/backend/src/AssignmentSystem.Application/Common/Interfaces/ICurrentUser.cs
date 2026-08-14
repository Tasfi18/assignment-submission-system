using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Common.Interfaces;

/// <summary>The authenticated caller, resolved from the JWT by the API layer.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    UserRole? Role { get; }

    bool IsAuthenticated { get; }

    /// <summary>Throws <see cref="Exceptions.UnauthorizedException"/> when there is no authenticated user.</summary>
    Guid RequireUserId();
}
