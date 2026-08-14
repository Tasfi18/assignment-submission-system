using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Contracts.Auth;
using AssignmentSystem.Application.Services;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Infrastructure.Security;
using AssignmentSystem.UnitTests.TestKit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssignmentSystem.UnitTests.Services;

public class AuthServiceTests
{
    private const string KnownPassword = "Password@123";

    [Fact]
    public async Task Signing_in_with_the_right_password_returns_a_token_and_the_profile()
    {
        using var context = new TestContext();
        var service = BuildAuthService(context, out _);

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = context.TeacherOne.Email,
            Password = KnownPassword
        });

        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.ExpiresAtUtc.Should().BeAfter(context.Clock.UtcNow);
        response.User.Email.Should().Be(context.TeacherOne.Email);
        response.User.RoleName.Should().Be("Teacher");
    }

    [Fact]
    public async Task The_email_is_matched_case_insensitively()
    {
        using var context = new TestContext();
        var service = BuildAuthService(context, out _);

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = context.TeacherOne.Email.ToUpperInvariant(),
            Password = KnownPassword
        });

        response.User.Id.Should().Be(context.TeacherOne.Id);
    }

    [Fact]
    public async Task A_wrong_password_is_rejected()
    {
        using var context = new TestContext();
        var service = BuildAuthService(context, out _);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(new LoginRequest
        {
            Email = context.TeacherOne.Email,
            Password = "not-the-password"
        }));
    }

    [Fact]
    public async Task An_unknown_email_fails_with_the_same_error_as_a_wrong_password()
    {
        using var context = new TestContext();
        var service = BuildAuthService(context, out _);

        // Identical exception type and message, so the endpoint cannot be used to discover accounts.
        var unknown = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "nobody@test.edu",
            Password = KnownPassword
        }));

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(new LoginRequest
        {
            Email = context.TeacherOne.Email,
            Password = "not-the-password"
        }));

        unknown.Message.Should().Be(wrongPassword.Message);
    }

    [Fact]
    public async Task A_deactivated_account_cannot_sign_in()
    {
        using var context = new TestContext();
        var service = BuildAuthService(context, out _);

        context.StudentOne.IsActive = false;
        await context.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.LoginAsync(new LoginRequest
        {
            Email = context.StudentOne.Email,
            Password = KnownPassword
        }));
    }

    [Fact]
    public async Task Students_cannot_sign_in_while_the_portal_is_switched_off()
    {
        using var context = new TestContext();
        var service = BuildAuthService(context, out _);

        context.GivenSetting(SettingKeys.StudentPortalEnabled, "false");

        await Assert.ThrowsAsync<ForbiddenException>(() => service.LoginAsync(new LoginRequest
        {
            Email = context.StudentOne.Email,
            Password = KnownPassword
        }));

        // Staff are unaffected by the student portal switch.
        var teacherLogin = await service.LoginAsync(new LoginRequest
        {
            Email = context.TeacherOne.Email,
            Password = KnownPassword
        });

        teacherLogin.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Changing_a_password_requires_the_current_one()
    {
        using var context = new TestContext();
        var service = BuildAuthService(context, out _);

        context.CurrentUser.SignInAs(context.StudentOne);

        await Assert.ThrowsAsync<ValidationException>(() => service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = "wrong",
            NewPassword = "Brand@New1"
        }));
    }

    [Fact]
    public async Task A_changed_password_replaces_the_old_one()
    {
        using var context = new TestContext();
        var service = BuildAuthService(context, out var hasher);

        context.CurrentUser.SignInAs(context.StudentOne);

        await service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = KnownPassword,
            NewPassword = "Brand@New1"
        });

        hasher.Verify("Brand@New1", context.StudentOne.PasswordHash).Should().BeTrue();
        hasher.Verify(KnownPassword, context.StudentOne.PasswordHash).Should().BeFalse();
    }

    private static AuthService BuildAuthService(TestContext context, out Pbkdf2PasswordHasher hasher)
    {
        hasher = new Pbkdf2PasswordHasher();

        foreach (var user in context.Db.Users.ToList())
        {
            user.PasswordHash = hasher.Hash(KnownPassword);
        }

        context.Db.SaveChanges();

        var options = Options.Create(new JwtOptions
        {
            Issuer = "AssignmentSystem.Tests",
            Audience = "AssignmentSystem.Tests",
            Key = "unit-test-signing-key-that-is-long-enough-1234",
            AccessTokenMinutes = 60
        });

        var tokenGenerator = new JwtTokenGenerator(options, context.Clock);

        return new AuthService(
            context.Db,
            hasher,
            tokenGenerator,
            context.CurrentUser,
            ServiceFactory.Settings(context),
            NullLogger<AuthService>.Instance);
    }
}

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void A_hash_never_contains_the_password()
    {
        var hash = _hasher.Hash("Correct@Horse1");

        hash.Should().NotContain("Correct@Horse1");
        hash.Should().StartWith("PBKDF2$sha256$100000$");
    }

    [Fact]
    public void The_same_password_hashes_differently_every_time()
    {
        // A fresh random salt per hash, so identical passwords are not identifiable in the database.
        _hasher.Hash("Correct@Horse1").Should().NotBe(_hasher.Hash("Correct@Horse1"));
    }

    [Fact]
    public void A_password_verifies_against_its_own_hash()
    {
        var hash = _hasher.Hash("Correct@Horse1");

        _hasher.Verify("Correct@Horse1", hash).Should().BeTrue();
        _hasher.Verify("correct@horse1", hash).Should().BeFalse();
        _hasher.Verify("something else", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("PBKDF2$sha256$100000$onlyfourparts")]
    [InlineData("PBKDF2$sha256$abc$c2FsdA==$aGFzaA==")]
    public void A_malformed_hash_is_rejected_instead_of_throwing(string malformed)
    {
        _hasher.Verify("Correct@Horse1", malformed).Should().BeFalse();
    }
}

public class JwtTokenGeneratorTests
{
    [Fact]
    public void The_generated_token_carries_the_identity_and_role_of_the_user()
    {
        using var context = new TestContext();

        var options = Options.Create(new JwtOptions
        {
            Issuer = "AssignmentSystem.Tests",
            Audience = "AssignmentSystem.Tests",
            Key = "unit-test-signing-key-that-is-long-enough-1234",
            AccessTokenMinutes = 60
        });

        var generator = new JwtTokenGenerator(options, context.Clock);

        var token = generator.Generate(context.StudentOne);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

        parsed.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be(context.StudentOne.Id.ToString());
        parsed.Claims.Single(c => c.Type == ClaimTypes.Role).Value.Should().Be("Student");
        parsed.Claims.Single(c => c.Type == JwtTokenGenerator.ClassRoomClaimType).Value.Should().Be(context.ClassTenA.Id.ToString());
        parsed.Issuer.Should().Be("AssignmentSystem.Tests");
        token.ExpiresAtUtc.Should().Be(context.Clock.UtcNow.AddMinutes(60));
    }

    [Fact]
    public void A_teacher_token_carries_no_class_claim()
    {
        using var context = new TestContext();

        var options = Options.Create(new JwtOptions
        {
            Issuer = "AssignmentSystem.Tests",
            Audience = "AssignmentSystem.Tests",
            Key = "unit-test-signing-key-that-is-long-enough-1234",
            AccessTokenMinutes = 60
        });

        var generator = new JwtTokenGenerator(options, context.Clock);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(generator.Generate(context.TeacherOne).AccessToken);

        parsed.Claims.Should().NotContain(c => c.Type == JwtTokenGenerator.ClassRoomClaimType);
    }
}
