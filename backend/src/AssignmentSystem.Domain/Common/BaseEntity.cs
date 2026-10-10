namespace AssignmentSystem.Domain.Common;

/// <summary>Base type for every persisted entity. Ids are client generated GUIDs.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
