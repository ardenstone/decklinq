using System.Diagnostics;

namespace backend.Services;

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
        var requestId = context.TraceIdentifier;
        context.Response.Headers["X-Correlation-ID"] = requestId;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["Method"] = context.Request.Method,
            ["Path"] = context.Request.Path.Value ?? string.Empty
        });

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Request started");

        try
        {
            await _next(context);
            _logger.LogInformation("Request completed with status {StatusCode} in {ElapsedMs}ms",
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed with status {StatusCode} in {ElapsedMs}ms",
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
