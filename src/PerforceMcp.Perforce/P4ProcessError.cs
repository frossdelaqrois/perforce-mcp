namespace PerforceMcp.Perforce;

public enum P4ProcessErrorCode
{
    NonZeroExit,
    TimedOut,
    StartFailed,
}

public sealed record P4ProcessError(P4ProcessErrorCode Code, string Message);
