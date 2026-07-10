namespace PerforceMcp.Perforce;

public sealed class P4ExecutableDiscoveryResult
{
    private P4ExecutableDiscoveryResult(
        string? executablePath,
        string? version,
        P4ExecutableDiscoveryError? error)
    {
        ExecutablePath = executablePath;
        Version = version;
        Error = error;
    }

    public bool IsSuccess => ExecutablePath is not null;

    public string? ExecutablePath { get; }

    public string? Version { get; }

    public P4ExecutableDiscoveryError? Error { get; }

    internal static P4ExecutableDiscoveryResult Success(
        string executablePath,
        string version) =>
        new(executablePath, version, null);

    internal static P4ExecutableDiscoveryResult Failure(
        P4ExecutableDiscoveryErrorCode code,
        string message) =>
        new(null, null, new P4ExecutableDiscoveryError(code, message));
}
