using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// All Fluent API configuration lives here so the whole relational model can be reviewed in one place.
/// Deletes are Restrict by default: academic records should never disappear because a parent row was
/// removed. The single exception is Assignment -&gt; Submission, where cascading is intentional.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(400);
        builder.Property(u => u.Role).IsRequired().HasConversion<int>();
        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");
        builder.HasIndex(u => u.Role).HasDatabaseName("IX_Users_Role");

        builder.HasOne(u => u.ClassRoom)
            .WithMany(c => c.Students)
            .HasForeignKey(u => u.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClassRoomConfiguration : IEntityTypeConfiguration<ClassRoom>
{
    public void Configure(EntityTypeBuilder<ClassRoom> builder)
    {
        builder.ToTable("ClassRooms");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(120);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(30);
        builder.Property(c => c.AcademicYear).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.IsActive).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique().HasDatabaseName("IX_ClassRooms_Code");
    }
}

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(120);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(30);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.IsActive).IsRequired();

        builder.HasIndex(s => s.Code).IsUnique().HasDatabaseName("IX_Subjects_Code");
    }
}

public class ClassSubjectTeacherConfiguration : IEntityTypeConfiguration<ClassSubjectTeacher>
{
    public void Configure(EntityTypeBuilder<ClassSubjectTeacher> builder)
    {
        builder.ToTable("ClassSubjectTeachers");
        builder.HasKey(t => t.Id);

        // One teacher may teach many subjects in many classes, but the same triple only once.
        builder.HasIndex(t => new { t.ClassRoomId, t.SubjectId, t.TeacherId })
            .IsUnique()
            .HasDatabaseName("IX_ClassSubjectTeachers_Class_Subject_Teacher");

        builder.HasOne(t => t.ClassRoom)
            .WithMany(c => c.TeacherAssignments)
            .HasForeignKey(t => t.ClassRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Subject)
            .WithMany(s => s.TeacherAssignments)
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Teacher)
            .WithMany(u => u.TeachingAssignments)
            .HasForeignKey(t => t.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Description).IsRequired().HasMaxLength(5000);
        builder.Property(a => a.MaxMarks).IsRequired().HasPrecision(6, 2);
        builder.Property(a => a.DeadlineUtc).IsRequired();
        builder.Property(a => a.Status).IsRequired().HasConversion<int>();
        builder.Property(a => a.AllowLateSubmission).IsRequired();
        builder.Property(a => a.AllowResubmission).IsRequired();

        builder.HasIndex(a => new { a.ClassRoomId, a.Status }).HasDatabaseName("IX_Assignments_ClassRoom_Status");
        builder.HasIndex(a => a.TeacherId).HasDatabaseName("IX_Assignments_TeacherId");
        builder.HasIndex(a => a.DeadlineUtc).HasDatabaseName("IX_Assignments_DeadlineUtc");

        builder.HasOne(a => a.ClassRoom)
            .WithMany(c => c.Assignments)
            .HasForeignKey(a => a.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Subject)
            .WithMany(s => s.Assignments)
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Teacher)
            .WithMany(u => u.CreatedAssignments)
            .HasForeignKey(a => a.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("Submissions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Content).IsRequired().HasMaxLength(20000);
        builder.Property(s => s.AttachmentUrl).HasMaxLength(500);
        builder.Property(s => s.SubmittedAtUtc).IsRequired();
        builder.Property(s => s.IsLate).IsRequired();
        builder.Property(s => s.RevisionCount).IsRequired();
        builder.Property(s => s.Status).IsRequired().HasConversion<int>();
        builder.Property(s => s.Marks).HasPrecision(6, 2);
        builder.Property(s => s.Feedback).HasMaxLength(2000);

        // The core integrity rule of the workflow: one answer per student per assignment.
        builder.HasIndex(s => new { s.AssignmentId, s.StudentId })
            .IsUnique()
            .HasDatabaseName("IX_Submissions_Assignment_Student");

        builder.HasIndex(s => s.Status).HasDatabaseName("IX_Submissions_Status");

        builder.HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Student)
            .WithMany(u => u.Submissions)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.GradedByUser)
            .WithMany()
            .HasForeignKey(s => s.GradedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings");
        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key).HasMaxLength(100);
        builder.Property(s => s.Value).IsRequired().HasMaxLength(500);
        builder.Property(s => s.Description).HasMaxLength(300);
        builder.Property(s => s.UpdatedAtUtc).IsRequired();
    }
}
