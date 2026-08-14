namespace AssignmentSystem.Application.Common.Interfaces;

/// <summary>
/// Wraps the clock so that deadline / late-submission rules can be tested deterministically.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
