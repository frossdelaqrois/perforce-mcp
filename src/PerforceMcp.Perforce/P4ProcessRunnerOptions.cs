namespace PerforceMcp.Perforce;

public sealed class P4ProcessRunnerOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public IReadOnlyCollection<string> SensitiveValues { get; init; } = [];
}
