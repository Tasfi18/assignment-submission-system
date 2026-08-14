using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Infrastructure.Persistence;
using AssignmentSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No database connection string was found. Set ConnectionStrings:DefaultConnection in appsettings.json " +
                "or the ConnectionStrings__DefaultConnection environment variable.");
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }

    /// <summary>
    /// Applies pending migrations and seeds the demo data. Called once at start-up so a reviewer only
    /// has to point the API at an empty PostgreSQL database.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider serviceProvider, bool applyMigrations, bool seed)
    {
        using var scope = serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DatabaseInitialiser");

        if (applyMigrations)
        {
            logger.LogInformation("Applying database migrations");
            await db.Database.MigrateAsync();
        }

        if (seed)
        {
            await DbSeeder.SeedAsync(db, hasher, logger);
        }
    }
}
