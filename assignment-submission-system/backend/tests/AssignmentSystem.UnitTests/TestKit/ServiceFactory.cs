using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssignmentSystem.UnitTests.TestKit;

/// <summary>
/// Builds the services under test against the in-memory context. The real SettingsService is used
/// rather than a stub, so the tests also cover the interaction between an assignment's own
/// "allow late" flag and the institution-wide switch.
/// </summary>
public static class ServiceFactory
{
    public static ISettingsService Settings(TestContext context) =>
        new SettingsService(context.Db, context.Clock, NullLogger<SettingsService>.Instance);

    public static AssignmentService Assignments(TestContext context) =>
        new(context.Db, context.CurrentUser, context.Clock, NullLogger<AssignmentService>.Instance);

    public static SubmissionService Submissions(TestContext context) =>
        new(context.Db, context.CurrentUser, context.Clock, Settings(context), NullLogger<SubmissionService>.Instance);

    public static AcademicService Academics(TestContext context) =>
        new(context.Db, context.CurrentUser, context.Clock, NullLogger<AcademicService>.Instance);

    public static UserService Users(TestContext context, IPasswordHasher hasher) =>
        new(context.Db, hasher, context.CurrentUser, context.Clock, NullLogger<UserService>.Instance);
}
