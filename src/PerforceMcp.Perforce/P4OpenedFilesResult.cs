namespace PerforceMcp.Perforce;

public enum P4OpenedFilesErrorCode
{
    MissingLogin,
    MissingClient,
    UnreachableServer,
    MalformedOutput,
    TimedOut,
    CommandFailed,
    InvalidRequest,
}

public sealed record P4OpenedFilesError(P4OpenedFilesErrorCode Code, string Message);

public sealed record P4OpenedFile(
    string DepotPath,
    string? LocalPath,
    string Action,
    string Changelist,
    string FileType,
    bool IsLocked,
    bool IsExclusiveOpen);

public sealed record P4OpenedFilesResult(
    IReadOnlyList<P4OpenedFile> Files,
    int Count,
    int Limit,
    bool IsTruncated,
    P4OpenedFilesError? Error)
{
    public bool IsSuccess => Error is null;

    internal static P4OpenedFilesResult Failure(
        int limit,
        P4OpenedFilesErrorCode code,
        string message) =>
        new([], 0, limit, false, new P4OpenedFilesError(code, message));
}
