using System.Net;
using System.Text.Json;

namespace AgroAdvisor.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation error");
            await WriteAsync(context, ex.Message, HttpStatusCode.BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogInformation(ex, "Resource not found");
            await WriteAsync(context, ex.Message, HttpStatusCode.NotFound);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized");
            await WriteAsync(context, ex.Message, HttpStatusCode.Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            var message = _environment.IsDevelopment() ? ex.Message : "Internal server error";
            await WriteAsync(context, message, HttpStatusCode.InternalServerError);
        }
    }

    private static Task WriteAsync(HttpContext context, string message, HttpStatusCode statusCode)
    {
        if (context.Response.HasStarted) return Task.CompletedTask;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
