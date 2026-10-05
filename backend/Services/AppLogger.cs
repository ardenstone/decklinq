namespace backend.Services;

public interface IAppLogger
{
    void LogInformation(string eventName, object? context = null, string? message = null);
    void LogWarning(string eventName, object? context = null, string? message = null);
    void LogError(Exception? exception, string eventName, object? context = null, string? message = null);
}

public class AppLogger : IAppLogger
{
    private readonly ILogger<AppLogger> _logger;

    public AppLogger(ILogger<AppLogger> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string eventName, object? context = null, string? message = null)
    {
        if (context is null)
        {
            _logger.LogInformation("{EventName} {Message}", eventName, message ?? "No additional context");
            return;
        }

        _logger.LogInformation("{EventName} {Message} {Context}", eventName, message ?? "No additional context", context);
    }

    public void LogWarning(string eventName, object? context = null, string? message = null)
    {
        if (context is null)
        {
            _logger.LogWarning("{EventName} {Message}", eventName, message ?? "No additional context");
            return;
        }

        _logger.LogWarning("{EventName} {Message} {Context}", eventName, message ?? "No additional context", context);
    }

    public void LogError(Exception? exception, string eventName, object? context = null, string? message = null)
    {
        if (context is null)
        {
            _logger.LogError(exception, "{EventName} {Message}", eventName, message ?? "No additional context");
            return;
        }

        _logger.LogError(exception, "{EventName} {Message} {Context}", eventName, message ?? "No additional context", context);
    }
}
