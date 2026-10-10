using AssignmentSystem.Domain.Common;

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// Join entity created by an admin: "teacher T teaches subject S in class C".
/// This single row is what the API uses to authorise every teacher action, so a teacher can never
/// touch a class or subject that was not handed to them.
/// </summary>
public class ClassSubjectTeacher : BaseEntity
{
    public Guid ClassRoomId { get; set; }

    public ClassRoom ClassRoom { get; set; } = null!;

    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    public Guid TeacherId { get; set; }

    public User Teacher { get; set; } = null!;
}
