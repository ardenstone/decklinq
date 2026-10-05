using backend.Services;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests;

public class LoggingServiceTests
{
    [Fact]
    public void AppLogger_ResolvesFromDI_AndCanEmitStructuredLogs()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IAppLogger, AppLogger>();

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<IAppLogger>();

        var exception = new InvalidOperationException("boom");

        logger.LogInformation("service-started", new { operation = "unit-test" });
        logger.LogError(exception, "service-failed", new { operation = "unit-test" });
    }
}
