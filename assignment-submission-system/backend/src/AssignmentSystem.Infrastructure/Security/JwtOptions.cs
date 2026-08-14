namespace AssignmentSystem.Infrastructure.Security;

/// <summary>Bound from the "Jwt" configuration section (or the Jwt__* environment variables).</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "AssignmentSystem";

    public string Audience { get; set; } = "AssignmentSystemClient";

    /// <summary>Signing key. Must be at least 32 characters for HMAC-SHA256.</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 480;
}
