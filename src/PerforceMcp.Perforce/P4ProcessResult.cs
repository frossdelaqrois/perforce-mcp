namespace PerforceMcp.Perforce;

public sealed record P4ProcessResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool StandardOutputTruncated,
    bool StandardErrorTruncated,
    TimeSpan Duration,
    P4ProcessError? Error)
{
    public bool IsSuccess => Error is null;
}
