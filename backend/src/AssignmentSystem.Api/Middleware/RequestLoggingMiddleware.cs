using System.Diagnostics;

namespace AssignmentSystem.Api.Middleware;

/// <summary>Structured one-line access log with the caller's identity and how long the request took.</summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var user = context.User?.Identity?.IsAuthenticated == true
                ? context.User.Identity!.Name ?? "authenticated"
                : "anonymous";

            _logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {Elapsed} ms for {User}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                user);
        }
    }
}
