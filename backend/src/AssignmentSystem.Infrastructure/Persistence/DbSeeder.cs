using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Infrastructure.Persistence;

/// <summary>
/// Seeds the demo data set described in the README.
///
/// The seeder is idempotent (it does nothing once accounts exist) and uses fixed GUIDs so the same
/// ids appear in the SQL script under /database. Deadlines are relative to "now" so the demo always
/// contains an assignment that is still open, one that has expired and one that is closed.
/// </summary>
public static class DbSeeder
{
    // --- classes
    private static readonly Guid ClassTenA = new("44444444-4444-4444-4444-000000000001");
    private static readonly Guid ClassNineB = new("44444444-4444-4444-4444-000000000002");

    // --- subjects
    private static readonly Guid SubjectMath = new("55555555-5555-5555-5555-000000000001");
    private static readonly Guid SubjectPhysics = new("55555555-5555-5555-5555-000000000002");
    private static readonly Guid SubjectIct = new("55555555-5555-5555-5555-000000000003");
    private static readonly Guid SubjectEnglish = new("55555555-5555-5555-5555-000000000004");

    // --- users
    private static readonly Guid AdminId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TeacherNusratId = new("22222222-2222-2222-2222-000000000001");
    private static readonly Guid TeacherRakibId = new("22222222-2222-2222-2222-000000000002");
    private static readonly Guid StudentTanvirId = new("33333333-3333-3333-3333-000000000001");
    private static readonly Guid StudentSadiaId = new("33333333-3333-3333-3333-000000000002");
    private static readonly Guid StudentMahdiId = new("33333333-3333-3333-3333-000000000003");
    private static readonly Guid StudentFarhanaId = new("33333333-3333-3333-3333-000000000004");
    private static readonly Guid StudentImranId = new("33333333-3333-3333-3333-000000000005");
    private static readonly Guid StudentNusaibaId = new("33333333-3333-3333-3333-000000000006");

    // --- assignments
    private static readonly Guid AssignmentQuadratic = new("77777777-7777-7777-7777-000000000001");
    private static readonly Guid AssignmentNewton = new("77777777-7777-7777-7777-000000000002");
    private static readonly Guid AssignmentTrigDraft = new("77777777-7777-7777-7777-000000000003");
    private static readonly Guid AssignmentGeometry = new("77777777-7777-7777-7777-000000000004");
    private static readonly Guid AssignmentPortfolio = new("77777777-7777-7777-7777-000000000005");
    private static readonly Guid AssignmentEssay = new("77777777-7777-7777-7777-000000000006");

    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedSettingsAsync(db, cancellationToken);

        if (await db.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already contains accounts, skipping demo seed");
            return;
        }

        var now = DateTime.UtcNow;

        logger.LogInformation("Seeding demo data");

        var classes = new[]
        {
            new ClassRoom
            {
                Id = ClassTenA, Name = "Class 10 - Section A", Code = "C10A", AcademicYear = now.Year,
                Description = "Science group, morning shift.", IsActive = true, CreatedAtUtc = now
            },
            new ClassRoom
            {
                Id = ClassNineB, Name = "Class 9 - Section B", Code = "C09B", AcademicYear = now.Year,
                Description = "General group, day shift.", IsActive = true, CreatedAtUtc = now
            }
        };

        var subjects = new[]
        {
            new Subject { Id = SubjectMath, Name = "Mathematics", Code = "MATH-101", Description = "Algebra, geometry and trigonometry.", IsActive = true, CreatedAtUtc = now },
            new Subject { Id = SubjectPhysics, Name = "Physics", Code = "PHY-101", Description = "Mechanics and waves.", IsActive = true, CreatedAtUtc = now },
            new Subject { Id = SubjectIct, Name = "Information & Communication Technology", Code = "ICT-101", Description = "Computing fundamentals and the web.", IsActive = true, CreatedAtUtc = now },
            new Subject { Id = SubjectEnglish, Name = "English", Code = "ENG-101", Description = "Language and literature.", IsActive = true, CreatedAtUtc = now }
        };

        var users = new[]
        {
            NewUser(AdminId, "System Administrator", "admin@school.edu", "Admin@123", UserRole.Admin, null, passwordHasher, now),
            NewUser(TeacherNusratId, "Nusrat Jahan", "teacher@school.edu", "Teacher@123", UserRole.Teacher, null, passwordHasher, now),
            NewUser(TeacherRakibId, "Rakib Hasan", "rakib.hasan@school.edu", "Teacher@123", UserRole.Teacher, null, passwordHasher, now),
            NewUser(StudentTanvirId, "Tanvir Ahmed", "student@school.edu", "Student@123", UserRole.Student, ClassTenA, passwordHasher, now),
            NewUser(StudentSadiaId, "Sadia Islam", "sadia.islam@school.edu", "Student@123", UserRole.Student, ClassTenA, passwordHasher, now),
            NewUser(StudentMahdiId, "Mahdi Hasan", "mahdi.hasan@school.edu", "Student@123", UserRole.Student, ClassTenA, passwordHasher, now),
            NewUser(StudentFarhanaId, "Farhana Akter", "farhana.akter@school.edu", "Student@123", UserRole.Student, ClassTenA, passwordHasher, now),
            NewUser(StudentImranId, "Imran Kabir", "imran.kabir@school.edu", "Student@123", UserRole.Student, ClassNineB, passwordHasher, now),
            NewUser(StudentNusaibaId, "Nusaiba Rahman", "nusaiba.rahman@school.edu", "Student@123", UserRole.Student, ClassNineB, passwordHasher, now)
        };

        var teachingAssignments = new[]
        {
            NewLink("66666666-6666-6666-6666-000000000001", ClassTenA, SubjectMath, TeacherNusratId, now),
            NewLink("66666666-6666-6666-6666-000000000002", ClassTenA, SubjectPhysics, TeacherNusratId, now),
            NewLink("66666666-6666-6666-6666-000000000003", ClassNineB, SubjectMath, TeacherNusratId, now),
            NewLink("66666666-6666-6666-6666-000000000004", ClassTenA, SubjectIct, TeacherRakibId, now),
            NewLink("66666666-6666-6666-6666-000000000005", ClassNineB, SubjectEnglish, TeacherRakibId, now)
        };

        var assignments = new[]
        {
            new Assignment
            {
                Id = AssignmentQuadratic,
                Title = "Quadratic Equations - Problem Set 3",
                Description = "Solve problems 1 to 12 from chapter 4. Show every step of your working. Answers without working will not receive full marks.",
                ClassRoomId = ClassTenA, SubjectId = SubjectMath, TeacherId = TeacherNusratId,
                MaxMarks = 50m, DeadlineUtc = now.AddDays(7),
                Status = AssignmentStatus.Published, AllowLateSubmission = true, AllowResubmission = true,
                PublishedAtUtc = now.AddDays(-3), CreatedAtUtc = now.AddDays(-3)
            },
            new Assignment
            {
                Id = AssignmentNewton,
                Title = "Newton's Laws - Lab Report",
                Description = "Write a full lab report on the trolley and pulley experiment: aim, apparatus, procedure, observations, calculations and conclusion.",
                ClassRoomId = ClassTenA, SubjectId = SubjectPhysics, TeacherId = TeacherNusratId,
                MaxMarks = 100m, DeadlineUtc = now.AddDays(14),
                Status = AssignmentStatus.Published, AllowLateSubmission = false, AllowResubmission = true,
                PublishedAtUtc = now.AddDays(-2), CreatedAtUtc = now.AddDays(-2)
            },
            new Assignment
            {
                Id = AssignmentTrigDraft,
                Title = "Trigonometry Revision Sheet",
                Description = "Draft assignment - still being prepared. Students must not be able to see this one.",
                ClassRoomId = ClassTenA, SubjectId = SubjectMath, TeacherId = TeacherNusratId,
                MaxMarks = 30m, DeadlineUtc = now.AddDays(21),
                Status = AssignmentStatus.Draft, AllowLateSubmission = false, AllowResubmission = true,
                PublishedAtUtc = null, CreatedAtUtc = now.AddDays(-1)
            },
            new Assignment
            {
                Id = AssignmentGeometry,
                Title = "Coordinate Geometry - Weekly Test",
                Description = "Answer all five questions on straight lines and distance between two points.",
                ClassRoomId = ClassNineB, SubjectId = SubjectMath, TeacherId = TeacherNusratId,
                MaxMarks = 40m, DeadlineUtc = now.AddDays(-3),
                Status = AssignmentStatus.Published, AllowLateSubmission = true, AllowResubmission = true,
                PublishedAtUtc = now.AddDays(-12), CreatedAtUtc = now.AddDays(-12)
            },
            new Assignment
            {
                Id = AssignmentPortfolio,
                Title = "HTML & CSS Portfolio Page",
                Description = "Build a single page portfolio with a header, an about section and a contact form. Submit the link to your hosted page.",
                ClassRoomId = ClassTenA, SubjectId = SubjectIct, TeacherId = TeacherRakibId,
                MaxMarks = 60m, DeadlineUtc = now.AddDays(10),
                Status = AssignmentStatus.Published, AllowLateSubmission = true, AllowResubmission = true,
                PublishedAtUtc = now.AddDays(-5), CreatedAtUtc = now.AddDays(-5)
            },
            new Assignment
            {
                Id = AssignmentEssay,
                Title = "Essay: My Favourite Book",
                Description = "Write 400 words about a book you enjoyed and explain what it taught you.",
                ClassRoomId = ClassNineB, SubjectId = SubjectEnglish, TeacherId = TeacherRakibId,
                MaxMarks = 25m, DeadlineUtc = now.AddDays(-10),
                Status = AssignmentStatus.Closed, AllowLateSubmission = false, AllowResubmission = false,
                PublishedAtUtc = now.AddDays(-25), CreatedAtUtc = now.AddDays(-25)
            }
        };

        var submissions = new[]
        {
            new Submission
            {
                Id = new Guid("88888888-8888-8888-8888-000000000001"),
                AssignmentId = AssignmentQuadratic, StudentId = StudentTanvirId,
                Content = "Question 1: x = 3 or x = -5 (factorised as (x-3)(x+5)=0). Question 2: discriminant = 49, so the roots are real and distinct... [full working attached]",
                AttachmentUrl = "https://example.com/uploads/tanvir-problem-set-3.pdf",
                SubmittedAtUtc = now.AddDays(-2), IsLate = false, RevisionCount = 0,
                Status = SubmissionStatus.Graded, Marks = 45m,
                Feedback = "Very good work. Question 9 has a sign error in the second line, otherwise the method is correct throughout.",
                GradedByUserId = TeacherNusratId, GradedAtUtc = now.AddDays(-1), CreatedAtUtc = now.AddDays(-2)
            },
            new Submission
            {
                Id = new Guid("88888888-8888-8888-8888-000000000002"),
                AssignmentId = AssignmentQuadratic, StudentId = StudentSadiaId,
                Content = "Question 1: factorised form (x-3)(x+5). Question 2: used the quadratic formula, roots are 3.5 and -2. Question 3 onwards in the attached file.",
                SubmittedAtUtc = now.AddDays(-1), IsLate = false, RevisionCount = 1,
                Status = SubmissionStatus.Submitted, CreatedAtUtc = now.AddDays(-1),
                LastUpdatedAtUtc = now.AddHours(-8)
            },
            new Submission
            {
                Id = new Guid("88888888-8888-8888-8888-000000000003"),
                AssignmentId = AssignmentNewton, StudentId = StudentMahdiId,
                Content = "Aim: to verify Newton's second law using a trolley and a pulley. Apparatus: trolley, ticker tape timer, slotted masses... Conclusion: acceleration was proportional to the applied force within experimental error.",
                SubmittedAtUtc = now.AddHours(-30), IsLate = false, RevisionCount = 0,
                Status = SubmissionStatus.UnderReview, CreatedAtUtc = now.AddHours(-30)
            },
            new Submission
            {
                Id = new Guid("88888888-8888-8888-8888-000000000004"),
                AssignmentId = AssignmentGeometry, StudentId = StudentImranId,
                Content = "Q1: distance = 13 units. Q2: the gradient is -2 so the line is decreasing. Q3: midpoint (4, 1). Sorry for the delay, I was unwell last week.",
                SubmittedAtUtc = now.AddDays(-1), IsLate = true, RevisionCount = 0,
                Status = SubmissionStatus.Submitted, CreatedAtUtc = now.AddDays(-1)
            },
            new Submission
            {
                Id = new Guid("88888888-8888-8888-8888-000000000005"),
                AssignmentId = AssignmentPortfolio, StudentId = StudentFarhanaId,
                Content = "My portfolio page is hosted at the attached link. It has a header, an about section and a contact form.",
                AttachmentUrl = "https://example.com/farhana-portfolio",
                SubmittedAtUtc = now.AddDays(-3), IsLate = false, RevisionCount = 0,
                Status = SubmissionStatus.ReturnedForRevision,
                Feedback = "Good start, but the contact form has no labels and the page is not responsive below 480px. Please fix both and submit again.",
                CreatedAtUtc = now.AddDays(-3)
            },
            new Submission
            {
                Id = new Guid("88888888-8888-8888-8888-000000000006"),
                AssignmentId = AssignmentEssay, StudentId = StudentNusaibaId,
                Content = "My favourite book is 'Aranyak' by Bibhutibhushan Bandyopadhyay. It taught me that a place can change a person as much as people do...",
                SubmittedAtUtc = now.AddDays(-12), IsLate = false, RevisionCount = 0,
                Status = SubmissionStatus.Graded, Marks = 20m,
                Feedback = "Clear structure and honest reflection. Watch the tense changes in the third paragraph.",
                GradedByUserId = TeacherRakibId, GradedAtUtc = now.AddDays(-9), CreatedAtUtc = now.AddDays(-12)
            }
        };

        db.ClassRooms.AddRange(classes);
        db.Subjects.AddRange(subjects);
        db.Users.AddRange(users);
        db.ClassSubjectTeachers.AddRange(teachingAssignments);
        db.Assignments.AddRange(assignments);
        db.Submissions.AddRange(submissions);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {Users} users, {Classes} classes, {Subjects} subjects, {Assignments} assignments and {Submissions} submissions",
            users.Length, classes.Length, subjects.Length, assignments.Length, submissions.Length);
    }

    private static async Task SeedSettingsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var defaults = new[]
        {
            new AppSetting
            {
                Key = SettingKeys.AllowLateSubmissionsGlobally, Value = "true",
                Description = "Master switch. When false, late submissions are refused even if an assignment allows them.",
                UpdatedAtUtc = DateTime.UtcNow
            },
            new AppSetting
            {
                Key = SettingKeys.MaxSubmissionLength, Value = "20000",
                Description = "Maximum number of characters accepted in a submitted answer.",
                UpdatedAtUtc = DateTime.UtcNow
            },
            new AppSetting
            {
                Key = SettingKeys.InstitutionName, Value = "Green Valley School & College",
                Description = "Institution name shown in the application header.",
                UpdatedAtUtc = DateTime.UtcNow
            },
            new AppSetting
            {
                Key = SettingKeys.StudentPortalEnabled, Value = "true",
                Description = "When false, students cannot sign in.",
                UpdatedAtUtc = DateTime.UtcNow
            }
        };

        var existingKeys = await db.AppSettings.Select(s => s.Key).ToListAsync(cancellationToken);

        var missing = defaults.Where(s => !existingKeys.Contains(s.Key)).ToList();

        if (missing.Count > 0)
        {
            db.AppSettings.AddRange(missing);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static User NewUser(
        Guid id,
        string fullName,
        string email,
        string password,
        UserRole role,
        Guid? classRoomId,
        IPasswordHasher hasher,
        DateTime now) => new()
        {
            Id = id,
            FullName = fullName,
            Email = email,
            PasswordHash = hasher.Hash(password),
            Role = role,
            IsActive = true,
            ClassRoomId = classRoomId,
            CreatedAtUtc = now
        };

    private static ClassSubjectTeacher NewLink(string id, Guid classRoomId, Guid subjectId, Guid teacherId, DateTime now) => new()
    {
        Id = new Guid(id),
        ClassRoomId = classRoomId,
        SubjectId = subjectId,
        TeacherId = teacherId,
        CreatedAtUtc = now
    };
}
