using AssignmentSystem.Application.Common.Interfaces;

namespace AssignmentSystem.Infrastructure.Security;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
