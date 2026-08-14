using System.Text;
using System.Text.Json.Serialization;
using AssignmentSystem.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AssignmentSystem.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public const string CorsPolicyName = "FrontendPolicy";

    /// <summary>Authorization policy names used by the controllers.</summary>
    public static class Policies
    {
        public const string AdminOnly = "AdminOnly";
        public const string TeacherOnly = "TeacherOnly";
        public const string StudentOnly = "StudentOnly";
        public const string TeacherOrAdmin = "TeacherOrAdmin";
    }

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // Enums travel as readable names ("Published") instead of magic numbers.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // Model binding / DataAnnotations failures are returned in the same envelope the
        // exception middleware uses, so the frontend only has to understand one error shape.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                var problem = new ValidationProblemDetails(errors)
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Please correct the highlighted fields and try again.",
                    Instance = context.HttpContext.Request.Path
                };

                return new BadRequestObjectResult(problem)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Assignment & Submission Management API",
                Version = "v1",
                Description =
                    "Role based API for schools and colleges. Teachers create assignments, students submit answers " +
                    "and teachers award marks and feedback. Sign in through /api/auth/login, then press Authorize " +
                    "and paste the returned access token."
            });

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste only the token; Swagger adds the \"Bearer\" prefix.",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [scheme] = Array.Empty<string>()
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.Key) || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing or too short. Provide at least 32 characters through appsettings.Development.json " +
                "or the Jwt__Key environment variable.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole("Admin"));
            options.AddPolicy(Policies.TeacherOnly, policy => policy.RequireRole("Teacher"));
            options.AddPolicy(Policies.StudentOnly, policy => policy.RequireRole("Student"));
            options.AddPolicy(Policies.TeacherOrAdmin, policy => policy.RequireRole("Teacher", "Admin"));
        });

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" };

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        return services;
    }
}
