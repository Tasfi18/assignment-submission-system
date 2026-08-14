using AssignmentSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentSystem.Application;

public static class DependencyInjection
{
    /// <summary>Registers every application service. All of them are stateless and scoped to a request.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAcademicService, AcademicService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<ISettingsService, SettingsService>();

        return services;
    }
}
