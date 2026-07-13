namespace PerforceMcp.Perforce;

public enum P4PendingChangelistsErrorCode
{
    MissingLogin,
    MissingClient,
    UnreachableServer,
    MalformedOutput,
    TimedOut,
    CommandFailed,
    InvalidRequest,
}

public sealed record P4PendingChangelistsError(P4PendingChangelistsErrorCode Code, string Message);

public sealed record P4PendingFile(
    string DepotPath,
    string? LocalPath,
    string Action,
    string FileType);

public sealed record P4PendingChangelist(
    string Id,
    int? Number,
    bool IsDefault,
    string Description,
    string Owner,
    string Client,
    string Status,
    DateTimeOffset? ModifiedTime,
    int FileCount,
    bool IsFileCountExact,
    IReadOnlyList<P4PendingFile> Files,
    bool FilesTruncated);

public sealed record P4PendingChangelistsResult(
    IReadOnlyList<P4PendingChangelist> Changelists,
    int Count,
    int Limit,
    bool IncludesFiles,
    int FileLimit,
    bool IsTruncated,
    P4PendingChangelistsError? Error)
{
    public bool IsSuccess => Error is null;

    internal static P4PendingChangelistsResult Failure(
        int limit,
        bool includeFiles,
        int fileLimit,
        P4PendingChangelistsErrorCode code,
        string message) =>
        new([], 0, limit, includeFiles, fileLimit, false, new P4PendingChangelistsError(code, message));
}
