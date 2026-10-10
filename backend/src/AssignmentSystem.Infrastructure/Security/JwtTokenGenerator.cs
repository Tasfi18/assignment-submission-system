using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AssignmentSystem.Infrastructure.Security;

/// <summary>
/// Issues signed HMAC-SHA256 access tokens.
///
/// Claim types are deliberately the full WS-Identity URIs (ClaimTypes.*) rather than the short JWT
/// names. Those URIs are unaffected by inbound claim mapping, so [Authorize(Roles = "...")] and
/// ClaimsPrincipal lookups behave identically no matter how the handler is configured.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    public const string ClassRoomClaimType = "classRoomId";

    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _clock;

    public JwtTokenGenerator(IOptions<JwtOptions> options, IDateTimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public GeneratedToken Generate(User user)
    {
        var expires = _clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.ClassRoomId.HasValue)
        {
            claims.Add(new Claim(ClassRoomClaimType, user.ClassRoomId.Value.ToString()));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _clock.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new GeneratedToken(accessToken, expires);
    }
}
