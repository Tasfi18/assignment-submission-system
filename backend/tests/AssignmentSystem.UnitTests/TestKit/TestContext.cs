using AssignmentSystem.Application.Common.Exceptions;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.UnitTests.TestKit;

/// <summary>A clock the tests can move so deadline and late-submission rules are deterministic.</summary>
public class FixedClock : IDateTimeProvider
{
    public FixedClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}

/// <summary>Stands in for the JWT-derived caller.</summary>
public class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public UserRole? Role { get; set; }

    public bool IsAuthenticated => UserId.HasValue;

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedException("You must be signed in to perform this action.");

    public void SignInAs(User user)
    {
        UserId = user.Id;
        Email = user.Email;
        Role = user.Role;
    }

    public void SignOut()
    {
        UserId = null;
        Email = null;
        Role = null;
    }
}

/// <summary>
/// Builds a small but complete academic graph shared by the service tests:
///
///   Class 10-A  - Mathematics  - taught by Teacher One
///   Class 10-A  - Physics      - taught by Teacher One
///   Class 9-B   - Mathematics  - taught by Teacher Two
///
///   Student One + Student Two are enrolled in Class 10-A, Student Three in Class 9-B.
/// </summary>
public class TestContext : IDisposable
{
    public TestContext(DateTime? now = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"assignment-tests-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        Db = new AppDbContext(options);
        Clock = new FixedClock(now ?? new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc));
        CurrentUser = new FakeCurrentUser();

        Seed();
    }

    public AppDbContext Db { get; }

    public FixedClock Clock { get; }

    public FakeCurrentUser CurrentUser { get; }

    public ClassRoom ClassTenA { get; private set; } = null!;

    public ClassRoom ClassNineB { get; private set; } = null!;

    public Subject Mathematics { get; private set; } = null!;

    public Subject Physics { get; private set; } = null!;

    public Subject Chemistry { get; private set; } = null!;

    public User Admin { get; private set; } = null!;

    public User TeacherOne { get; private set; } = null!;

    public User TeacherTwo { get; private set; } = null!;

    public User StudentOne { get; private set; } = null!;

    public User StudentTwo { get; private set; } = null!;

    public User StudentThree { get; private set; } = null!;

    private void Seed()
    {
        var now = Clock.UtcNow;

        ClassTenA = new ClassRoom { Id = Guid.NewGuid(), Name = "Class 10 - A", Code = "C10A", AcademicYear = 2026, CreatedAtUtc = now };
        ClassNineB = new ClassRoom { Id = Guid.NewGuid(), Name = "Class 9 - B", Code = "C09B", AcademicYear = 2026, CreatedAtUtc = now };

        Mathematics = new Subject { Id = Guid.NewGuid(), Name = "Mathematics", Code = "MATH", CreatedAtUtc = now };
        Physics = new Subject { Id = Guid.NewGuid(), Name = "Physics", Code = "PHY", CreatedAtUtc = now };
        Chemistry = new Subject { Id = Guid.NewGuid(), Name = "Chemistry", Code = "CHEM", CreatedAtUtc = now };

        Admin = NewUser("Admin User", "admin@test.edu", UserRole.Admin, null, now);
        TeacherOne = NewUser("Teacher One", "teacher.one@test.edu", UserRole.Teacher, null, now);
        TeacherTwo = NewUser("Teacher Two", "teacher.two@test.edu", UserRole.Teacher, null, now);
        StudentOne = NewUser("Student One", "student.one@test.edu", UserRole.Student, ClassTenA.Id, now);
        StudentTwo = NewUser("Student Two", "student.two@test.edu", UserRole.Student, ClassTenA.Id, now);
        StudentThree = NewUser("Student Three", "student.three@test.edu", UserRole.Student, ClassNineB.Id, now);

        Db.ClassRooms.AddRange(ClassTenA, ClassNineB);
        Db.Subjects.AddRange(Mathematics, Physics, Chemistry);
        Db.Users.AddRange(Admin, TeacherOne, TeacherTwo, StudentOne, StudentTwo, StudentThree);

        Db.ClassSubjectTeachers.AddRange(
            NewLink(ClassTenA.Id, Mathematics.Id, TeacherOne.Id, now),
            NewLink(ClassTenA.Id, Physics.Id, TeacherOne.Id, now),
            NewLink(ClassNineB.Id, Mathematics.Id, TeacherTwo.Id, now));

        Db.SaveChanges();
    }

    /// <summary>Adds an assignment owned by <see cref="TeacherOne"/> unless another teacher is given.</summary>
    public Assignment GivenAssignment(
        AssignmentStatus status = AssignmentStatus.Published,
        Guid? classRoomId = null,
        Guid? subjectId = null,
        Guid? teacherId = null,
        decimal maxMarks = 100m,
        DateTime? deadlineUtc = null,
        bool allowLateSubmission = false,
        bool allowResubmission = true)
    {
        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            Title = "Sample assignment",
            Description = "Answer every question.",
            ClassRoomId = classRoomId ?? ClassTenA.Id,
            SubjectId = subjectId ?? Mathematics.Id,
            TeacherId = teacherId ?? TeacherOne.Id,
            MaxMarks = maxMarks,
            DeadlineUtc = deadlineUtc ?? Clock.UtcNow.AddDays(7),
            Status = status,
            AllowLateSubmission = allowLateSubmission,
            AllowResubmission = allowResubmission,
            PublishedAtUtc = status == AssignmentStatus.Draft ? null : Clock.UtcNow.AddDays(-1),
            CreatedAtUtc = Clock.UtcNow.AddDays(-1)
        };

        Db.Assignments.Add(assignment);
        Db.SaveChanges();

        return assignment;
    }

    public Submission GivenSubmission(
        Assignment assignment,
        User student,
        SubmissionStatus status = SubmissionStatus.Submitted,
        decimal? marks = null,
        bool isLate = false)
    {
        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StudentId = student.Id,
            Content = "My answer.",
            SubmittedAtUtc = Clock.UtcNow.AddHours(-2),
            IsLate = isLate,
            Status = status,
            Marks = marks,
            GradedByUserId = marks.HasValue ? assignment.TeacherId : null,
            GradedAtUtc = marks.HasValue ? Clock.UtcNow.AddHours(-1) : null,
            CreatedAtUtc = Clock.UtcNow.AddHours(-2)
        };

        Db.Submissions.Add(submission);
        Db.SaveChanges();

        return submission;
    }

    public void GivenSetting(string key, string value)
    {
        Db.AppSettings.Add(new AppSetting { Key = key, Value = value, UpdatedAtUtc = Clock.UtcNow });
        Db.SaveChanges();
    }

    private static User NewUser(string name, string email, UserRole role, Guid? classRoomId, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        FullName = name,
        Email = email,
        // Not a real hash: no test in this project exercises the hasher through this fixture.
        PasswordHash = "not-a-real-hash",
        Role = role,
        IsActive = true,
        ClassRoomId = classRoomId,
        CreatedAtUtc = now
    };

    private static ClassSubjectTeacher NewLink(Guid classRoomId, Guid subjectId, Guid teacherId, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        ClassRoomId = classRoomId,
        SubjectId = subjectId,
        TeacherId = teacherId,
        CreatedAtUtc = now
    };

    public void Dispose()
    {
        Db.Database.EnsureDeleted();
        Db.Dispose();
        GC.SuppressFinalize(this);
    }
}
