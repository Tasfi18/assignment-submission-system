using System.Text.Json;
using AssignmentSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Api.Middleware;

/// <summary>
/// Translates the application's exception vocabulary into RFC 7807 problem responses, so controllers
/// never need try/catch and the frontend always receives the same error shape.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Authentication failed."),
            ForbiddenException => (StatusCodes.Status403Forbidden, "You are not allowed to perform this action."),
            NotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
            ConflictException => (StatusCodes.Status409Conflict, "The request conflicts with existing data."),
            BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "The request was rejected by a business rule."),
            DbUpdateException => (StatusCodes.Status409Conflict, "The request conflicts with existing data."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "{Status} on {Method} {Path}: {Message}",
                status, context.Request.Method, context.Request.Path, exception.Message);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = context.Request.Path,
            Detail = status == StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
                ? "Please try again. If the problem persists, contact the administrator."
                : exception.Message
        };

        if (exception is ValidationException validation && validation.Errors.Count > 0)
        {
            problem.Extensions["errors"] = validation.Errors;
        }

        if (_environment.IsDevelopment() && status == StatusCodes.Status500InternalServerError)
        {
            problem.Extensions["exception"] = exception.GetType().Name;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var payload = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(payload);
    }
}
