using AssignmentSystem.Domain.Entities;

namespace AssignmentSystem.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    GeneratedToken Generate(User user);
}

/// <summary>A signed access token together with its absolute expiry.</summary>
public record GeneratedToken(string AccessToken, DateTime ExpiresAtUtc);
