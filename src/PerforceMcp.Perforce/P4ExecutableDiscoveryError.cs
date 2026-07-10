namespace PerforceMcp.Perforce;

public enum P4ExecutableDiscoveryErrorCode
{
    NotFound,
    InvalidExecutable,
    ValidationFailed,
    ValidationTimedOut,
    StartFailed,
}

public sealed record P4ExecutableDiscoveryError(
    P4ExecutableDiscoveryErrorCode Code,
    string Message);
