using AssignmentSystem.Domain.Common;

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// A class or course (for example "Class 10 - Section A" or "CSE-3101"). Students belong to exactly
/// one of these, and assignments are always targeted at one class + one subject.
/// </summary>
public class ClassRoom : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int AcademicYear { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<User> Students { get; set; } = new List<User>();

    public ICollection<ClassSubjectTeacher> TeacherAssignments { get; set; } = new List<ClassSubjectTeacher>();

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
