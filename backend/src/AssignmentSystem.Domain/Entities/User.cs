using AssignmentSystem.Domain.Common;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// A single account table is used for all three roles. The <see cref="Role"/> discriminator keeps
/// authorisation simple and avoids duplicating name/email/credential columns across three tables.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Only meaningful for students: the class/course the student is enrolled in.</summary>
    public Guid? ClassRoomId { get; set; }

    public ClassRoom? ClassRoom { get; set; }

    public ICollection<ClassSubjectTeacher> TeachingAssignments { get; set; } = new List<ClassSubjectTeacher>();

    public ICollection<Assignment> CreatedAssignments { get; set; } = new List<Assignment>();

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
