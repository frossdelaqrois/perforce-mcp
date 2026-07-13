using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using PerforceMcp.Server;

namespace PerforceMcp.Server.Tests;

public sealed class StdioLoggingTests
{
    [Fact]
    public void RoutesEveryConsoleLogLevelToStandardError()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        StdioLogging.Configure(builder.Logging);

        using IHost host = builder.Build();
        ConsoleLoggerOptions options = host.Services
            .GetRequiredService<IOptions<ConsoleLoggerOptions>>()
            .Value;

        Assert.Equal(LogLevel.Trace, options.LogToStandardErrorThreshold);
    }
}
