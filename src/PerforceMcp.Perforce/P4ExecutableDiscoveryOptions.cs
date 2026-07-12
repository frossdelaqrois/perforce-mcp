namespace PerforceMcp.Perforce;

public sealed class P4ExecutableDiscoveryOptions
{
    public string? ExecutablePath { get; init; }

    public TimeSpan ValidationTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
