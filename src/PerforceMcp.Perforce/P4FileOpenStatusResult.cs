namespace PerforceMcp.Perforce;

public enum P4FileOpenStatusErrorCode
{
    InvalidRequest,
    MissingFile,
    MissingLogin,
    MissingClient,
    UnreachableServer,
    TimedOut,
    MalformedOutput,
    CommandFailed,
}

public sealed record P4FileOpenStatusError(P4FileOpenStatusErrorCode Code, string Message);

public sealed record P4FileOpenRecord(
    string User,
    string Client,
    string Action,
    string Changelist,
    string FileType,
    bool IsLocked,
    bool IsExclusiveOpen,
    bool IsOpenedByCurrentUser,
    bool BlocksCurrentUser);

public sealed record P4FileOpenMatch(
    string DepotPath,
    string? LocalPath,
    string FileType,
    bool IsUnrealAsset,
    bool IsOpen,
    bool IsBlocking,
    string BlockingReason,
    IReadOnlyList<P4FileOpenRecord> Opens);

public sealed record P4FileOpenStatusResult(
    string Query,
    IReadOnlyList<P4FileOpenMatch> Matches,
    int Count,
    int Limit,
    bool IsAmbiguous,
    bool IsTruncated,
    bool OpenRecordsTruncated,
    P4FileOpenStatusError? Error)
{
    public bool IsSuccess => Error is null;

    internal static P4FileOpenStatusResult Failure(string query, int limit, P4FileOpenStatusErrorCode code, string message) =>
        new(query, [], 0, limit, false, false, false, new(code, message));
}
