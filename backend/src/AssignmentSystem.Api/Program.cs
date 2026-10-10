using AssignmentSystem.Api.Extensions;
using AssignmentSystem.Api.Middleware;
using AssignmentSystem.Application;
using AssignmentSystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// ---------------------------------------------------------------- database
var applyMigrations = builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true);
var seedDemoData = builder.Configuration.GetValue("Database:SeedDemoData", true);

if (applyMigrations || seedDemoData)
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    // In Docker the API usually wins the race against PostgreSQL, so give the database a few tries.
    const int maxAttempts = 10;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await app.Services.InitialiseDatabaseAsync(applyMigrations, seedDemoData);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            startupLogger.LogWarning(
                ex, "Database is not ready yet (attempt {Attempt}/{MaxAttempts}). Retrying in 3 seconds",
                attempt, maxAttempts);

            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

// ---------------------------------------------------------------- pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (builder.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Assignment & Submission Management API v1");
        options.DocumentTitle = "Assignment & Submission Management API";
        options.DisplayRequestDuration();
    });
}

app.UseCors(ApiServiceCollectionExtensions.CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    utcNow = DateTime.UtcNow
})).WithName("HealthCheck").AllowAnonymous();

app.Run();

/// <summary>Exposed so a future integration test project can spin the API up with WebApplicationFactory.</summary>
public partial class Program
{
}
