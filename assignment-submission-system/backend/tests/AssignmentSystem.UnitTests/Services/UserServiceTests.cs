using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Contracts.Academics;
using AssignmentSystem.Application.Contracts.Users;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Security;
using AssignmentSystem.UnitTests.TestKit;

namespace AssignmentSystem.UnitTests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task A_student_must_be_given_a_class()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Users(context, new Pbkdf2PasswordHasher());

        var request = new CreateUserRequest
        {
            FullName = "New Student",
            Email = "new.student@test.edu",
            Password = "Student@123",
            Role = UserRole.Student,
            ClassRoomId = null
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task A_teacher_is_never_attached_to_a_class()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Users(context, new Pbkdf2PasswordHasher());

        var created = await service.CreateAsync(new CreateUserRequest
        {
            FullName = "New Teacher",
            Email = "New.Teacher@test.edu",
            Password = "Teacher@123",
            Role = UserRole.Teacher,
            ClassRoomId = context.ClassTenA.Id
        });

        created.ClassRoomId.Should().BeNull();
        // Emails are normalised so that logging in is not case sensitive.
        created.Email.Should().Be("new.teacher@test.edu");
    }

    [Fact]
    public async Task An_email_can_only_be_used_once()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Users(context, new Pbkdf2PasswordHasher());

        var request = new CreateUserRequest
        {
            FullName = "Duplicate",
            Email = context.TeacherOne.Email,
            Password = "Teacher@123",
            Role = UserRole.Teacher
        };

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task An_administrator_cannot_deactivate_their_own_account()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Users(context, new Pbkdf2PasswordHasher());

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SetActiveStateAsync(context.Admin.Id, false));
    }

    [Fact]
    public async Task An_account_with_academic_history_cannot_be_deleted()
    {
        using var context = new TestContext();

        var assignment = context.GivenAssignment(AssignmentStatus.Published);
        context.GivenSubmission(assignment, context.StudentOne);

        context.CurrentUser.SignInAs(context.Admin);
        var service = ServiceFactory.Users(context, new Pbkdf2PasswordHasher());

        // Deleting the student would take their submitted work with it, so deactivation is the only option.
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(context.StudentOne.Id));
    }

    [Fact]
    public async Task An_unused_account_is_deleted()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Users(context, new Pbkdf2PasswordHasher());

        await service.DeleteAsync(context.StudentTwo.Id);

        context.Db.Users.Any(u => u.Id == context.StudentTwo.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Accounts_can_be_filtered_by_role_and_class()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Users(context, new Pbkdf2PasswordHasher());

        var students = await service.GetAsync(new UserQuery { Role = UserRole.Student, ClassRoomId = context.ClassTenA.Id });

        students.TotalCount.Should().Be(2);
        students.Items.Should().OnlyContain(u => u.RoleName == "Student");
    }
}

public class AcademicServiceTests
{
    [Fact]
    public async Task Only_a_teacher_account_can_be_mapped_to_a_class_and_subject()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Academics(context);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateTeachingAssignmentAsync(new CreateTeachingAssignmentRequest
        {
            ClassRoomId = context.ClassTenA.Id,
            SubjectId = context.Chemistry.Id,
            TeacherId = context.StudentOne.Id
        }));
    }

    [Fact]
    public async Task The_same_teacher_cannot_be_mapped_twice_to_one_class_and_subject()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Academics(context);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateTeachingAssignmentAsync(new CreateTeachingAssignmentRequest
        {
            ClassRoomId = context.ClassTenA.Id,
            SubjectId = context.Mathematics.Id,
            TeacherId = context.TeacherOne.Id
        }));
    }

    [Fact]
    public async Task A_teacher_reads_back_only_their_own_mappings()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.TeacherOne);

        var service = ServiceFactory.Academics(context);

        var mine = await service.GetMyTeachingAssignmentsAsync();

        mine.Should().HaveCount(2);
        mine.Should().OnlyContain(t => t.TeacherName == "Teacher One");
        mine.Select(t => t.SubjectName).Should().BeEquivalentTo(new[] { "Mathematics", "Physics" });
    }

    [Fact]
    public async Task A_class_that_still_has_students_cannot_be_deleted()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Academics(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteClassRoomAsync(context.ClassTenA.Id));
    }

    [Fact]
    public async Task A_class_code_is_unique_and_stored_in_upper_case()
    {
        using var context = new TestContext();
        context.CurrentUser.SignInAs(context.Admin);

        var service = ServiceFactory.Academics(context);

        var created = await service.CreateClassRoomAsync(new CreateClassRoomRequest
        {
            Name = "Class 11 - C",
            Code = "c11c",
            AcademicYear = 2026
        });

        created.Code.Should().Be("C11C");

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateClassRoomAsync(new CreateClassRoomRequest
        {
            Name = "Duplicate",
            Code = "C11C",
            AcademicYear = 2026
        }));
    }
}
