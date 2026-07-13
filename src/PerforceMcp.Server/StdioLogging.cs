using Microsoft.Extensions.Logging;

namespace PerforceMcp.Server;

internal static class StdioLogging
{
    internal static void Configure(ILoggingBuilder logging)
    {
        ArgumentNullException.ThrowIfNull(logging);

        logging.ClearProviders();
        logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
    }
}
