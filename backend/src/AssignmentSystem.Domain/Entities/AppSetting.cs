namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// Application level settings maintained by the admin. Kept as a key/value table so new switches can
/// be added without a schema change.
/// </summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
