namespace PerforceMcp.Perforce;

public enum P4InfoErrorCode
{
    ExecutableUnavailable,
    MissingLogin,
    MissingClient,
    UnreachableServer,
    MalformedOutput,
    TimedOut,
    CommandFailed,
}

public sealed record P4InfoError(P4InfoErrorCode Code, string Message);

public sealed record P4InfoResult(
    string? ServerAddress,
    string? User,
    string? Client,
    string? ClientRoot,
    string? ServerVersion,
    P4InfoError? Error)
{
    public bool IsSuccess => Error is null;

    internal static P4InfoResult Failure(P4InfoErrorCode code, string message) =>
        new(null, null, null, null, null, new P4InfoError(code, message));
}
