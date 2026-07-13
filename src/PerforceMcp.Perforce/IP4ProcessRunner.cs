namespace PerforceMcp.Perforce;

internal interface IP4ProcessRunner
{
    Task<P4ProcessResult> RunAsync(
        IReadOnlyList<string> arguments,
        P4ProcessRunnerOptions? options = null,
        CancellationToken cancellationToken = default);
}
