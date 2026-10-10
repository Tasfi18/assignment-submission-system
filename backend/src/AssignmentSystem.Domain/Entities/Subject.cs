using AssignmentSystem.Domain.Common;

namespace AssignmentSystem.Domain.Entities;

/// <summary>A subject taught in one or more classes (Mathematics, Physics, ...).</summary>
public class Subject : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ClassSubjectTeacher> TeacherAssignments { get; set; } = new List<ClassSubjectTeacher>();

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
