using System.Globalization;

namespace PerforceMcp.Perforce;

public sealed class P4FileOpenStatusService
{
    public const int DefaultLimit = 10;
    public const int MaximumLimit = 25;
    private const int MaximumOpenRecords = 200;
    private readonly IP4ProcessRunner _runner;

    public P4FileOpenStatusService(P4ExecutableDiscoveryResult executable) : this(new P4ProcessRunner(executable)) { }
    internal P4FileOpenStatusService(IP4ProcessRunner runner) => _runner = runner;

    public async Task<P4FileOpenStatusResult> GetAsync(string query, int limit = DefaultLimit, CancellationToken cancellationToken = default)
    {
        string normalizedQuery = query?.Trim() ?? string.Empty;
        if (limit is < 1 or > MaximumLimit || string.IsNullOrWhiteSpace(normalizedQuery) || ContainsWildcard(normalizedQuery))
        {
            return P4FileOpenStatusResult.Failure(normalizedQuery, limit, P4FileOpenStatusErrorCode.InvalidRequest,
                $"Provide a file path or exact filename without wildcards; limit must be between 1 and {MaximumLimit}.");
        }

        P4ProcessResult infoResult = await RunAsync(["-ztag", "info"], cancellationToken).ConfigureAwait(false);
        if (TryFailure(infoResult, normalizedQuery, limit, out P4FileOpenStatusResult? failure)) return failure!;
        Dictionary<string, string> info = ParseInfo(infoResult.StandardOutput);
        if (!Required(info, "userName", out string? currentUser))
        {
            return P4FileOpenStatusResult.Failure(normalizedQuery, limit, P4FileOpenStatusErrorCode.MalformedOutput,
                "Perforce returned incomplete workspace information.");
        }
        if (!Required(info, "clientName", out _) || !Required(info, "clientRoot", out string? clientRoot))
        {
            return P4FileOpenStatusResult.Failure(normalizedQuery, limit, P4FileOpenStatusErrorCode.MissingClient,
                "No valid Perforce client/workspace is configured for this environment.");
        }

        bool isDepotPath = normalizedQuery.StartsWith("//", StringComparison.Ordinal);
        bool isExactName = !isDepotPath && Path.GetFileName(normalizedQuery) == normalizedQuery;
        string fileSpec = normalizedQuery;
        bool candidatesTruncated = false;

        if (!isDepotPath && !isExactName)
        {
            string fullPath;
            try { fullPath = Path.GetFullPath(normalizedQuery, clientRoot!); }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return P4FileOpenStatusResult.Failure(normalizedQuery, limit, P4FileOpenStatusErrorCode.InvalidRequest, "The workspace path is invalid.");
            }
            if (!IsWithinRoot(fullPath, clientRoot!))
            {
                return P4FileOpenStatusResult.Failure(normalizedQuery, limit, P4FileOpenStatusErrorCode.InvalidRequest,
                    "Local paths must be inside the active Perforce workspace.");
            }
            fileSpec = fullPath;
        }

        if (isExactName)
        {
            P4ProcessResult filesResult = await RunAsync(["-ztag", "files", "-m", (limit + 1).ToString(CultureInfo.InvariantCulture), $"//.../{normalizedQuery}"], cancellationToken).ConfigureAwait(false);
            if (TryFailure(filesResult, normalizedQuery, limit, out failure, allowNoSuchFile: true)) return failure!;
            List<Dictionary<string, string>> fileRecords = ParseTagged(filesResult.StandardOutput);
            if (IsMalformedTagged(filesResult.StandardOutput, fileRecords)) return Malformed(normalizedQuery, limit);
            string[] depotCandidates = fileRecords
                .Select(record => record.GetValueOrDefault("depotFile"))
                .Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToArray();
            candidatesTruncated = depotCandidates.Length > limit;
            depotCandidates = depotCandidates.Take(limit).ToArray();
            if (depotCandidates.Length == 0) return Missing(normalizedQuery, limit);
            fileSpec = string.Join('\n', depotCandidates);
        }

        string[] specs = fileSpec.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var fstatArguments = new List<string> { "-ztag", "fstat", "-m", (limit + 1).ToString(CultureInfo.InvariantCulture), "-T", "depotFile,clientFile,headType" };
        fstatArguments.AddRange(specs);
        P4ProcessResult fstatResult = await RunAsync(fstatArguments, cancellationToken).ConfigureAwait(false);
        if (TryFailure(fstatResult, normalizedQuery, limit, out failure, allowNoSuchFile: true)) return failure!;
        List<Dictionary<string, string>> metadata = ParseTagged(fstatResult.StandardOutput);
        if (IsMalformedTagged(fstatResult.StandardOutput, metadata)) return Malformed(normalizedQuery, limit);
        if (metadata.Count == 0) return Missing(normalizedQuery, limit);
        if (metadata.Any(record => !Required(record, "depotFile", out _) || !Required(record, "headType", out _)))
            return Malformed(normalizedQuery, limit);

        bool fstatTruncated = metadata.Count > limit;
        metadata = metadata.Take(limit).ToList();
        var openedArguments = new List<string> { "-ztag", "opened", "-a", "-m", (MaximumOpenRecords + 1).ToString(CultureInfo.InvariantCulture) };
        openedArguments.AddRange(metadata.Select(record => record["depotFile"]));
        P4ProcessResult openedResult = await RunAsync(openedArguments, cancellationToken).ConfigureAwait(false);
        if (TryFailure(openedResult, normalizedQuery, limit, out failure, allowNotOpened: true)) return failure!;
        List<Dictionary<string, string>> opened = ParseTagged(openedResult.StandardOutput);
        if (IsMalformedTagged(openedResult.StandardOutput, opened)) return Malformed(normalizedQuery, limit);
        if (opened.Any(record => !Required(record, "depotFile", out _) || !Required(record, "user", out _) ||
            !Required(record, "client", out _) || !Required(record, "action", out _) ||
            !Required(record, "change", out _) || !Required(record, "type", out _))) return Malformed(normalizedQuery, limit);

        bool openRecordsTruncated = opened.Count > MaximumOpenRecords;
        opened = opened.Take(MaximumOpenRecords).ToList();
        var matches = metadata.Select(file => BuildMatch(file, opened, currentUser!)).ToArray();
        bool ambiguous = isExactName && (matches.Length > 1 || candidatesTruncated || fstatTruncated);
        return new(normalizedQuery, matches, matches.Length, limit, ambiguous, candidatesTruncated || fstatTruncated, openRecordsTruncated, null);
    }

    private static P4FileOpenMatch BuildMatch(Dictionary<string, string> file, List<Dictionary<string, string>> opened, string currentUser)
    {
        string depotPath = file["depotFile"];
        string fileType = file["headType"];
        P4FileOpenRecord[] records = opened.Where(record => string.Equals(record["depotFile"], depotPath, StringComparison.Ordinal))
            .Select(record =>
            {
                string openType = record["type"];
                bool isCurrent = string.Equals(record["user"], currentUser, StringComparison.OrdinalIgnoreCase);
                bool exclusive = IsExclusive(openType);
                bool locked = Truthy(record, "locked") || Truthy(record, "lock") || Truthy(record, "ourLock");
                return new P4FileOpenRecord(record["user"], record["client"], record["action"], record["change"], openType,
                    locked, exclusive, isCurrent, !isCurrent && (exclusive || locked));
            }).ToArray();
        bool blocking = records.Any(record => record.BlocksCurrentUser);
        string reason = blocking ? "Another user has an exclusive-open or locked record for this file."
            : records.Length == 0 ? "The file is not open by any visible user."
            : records.All(record => record.IsOpenedByCurrentUser) ? "The file is open only by the current user."
            : "The file is open, but no visible open record is exclusive or locked.";
        string extension = Path.GetExtension(depotPath);
        bool unreal = extension.Equals(".uasset", StringComparison.OrdinalIgnoreCase) || extension.Equals(".umap", StringComparison.OrdinalIgnoreCase);
        return new(depotPath, file.GetValueOrDefault("clientFile"), fileType, unreal, records.Length > 0, blocking, reason, records);
    }

    private async Task<P4ProcessResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken token) =>
        await _runner.RunAsync(arguments, cancellationToken: token).ConfigureAwait(false);

    private static bool TryFailure(P4ProcessResult process, string query, int limit, out P4FileOpenStatusResult? failure,
        bool allowNoSuchFile = false, bool allowNotOpened = false)
    {
        failure = null;
        if (process.Error?.Code == P4ProcessErrorCode.TimedOut) { failure = P4FileOpenStatusResult.Failure(query, limit, P4FileOpenStatusErrorCode.TimedOut, "The Perforce server did not respond before the timeout."); return true; }
        if (process.IsSuccess && !process.StandardOutputTruncated) return false;
        string output = string.Concat(process.StandardOutput, "\n", process.StandardError);
        if (allowNoSuchFile && ContainsAny(output, "no such file", "file(s) not in client view")) return false;
        if (allowNotOpened && ContainsAny(output, "file(s) not opened", "no file(s) opened")) return false;
        P4FileOpenStatusErrorCode code = process.StandardOutputTruncated ? P4FileOpenStatusErrorCode.MalformedOutput
            : ContainsAny(output, "password invalid", "please login", "session has expired") ? P4FileOpenStatusErrorCode.MissingLogin
            : ContainsAny(output, "connect to server failed", "tcp connect") ? P4FileOpenStatusErrorCode.UnreachableServer
            : ContainsAny(output, "client unknown", "no client name") ? P4FileOpenStatusErrorCode.MissingClient
            : P4FileOpenStatusErrorCode.CommandFailed;
        failure = P4FileOpenStatusResult.Failure(query, limit, code, code == P4FileOpenStatusErrorCode.MalformedOutput ? "Perforce returned more machine-readable data than could be safely processed." : "File open status could not be retrieved.");
        return true;
    }

    private static List<Dictionary<string, string>> ParseTagged(string output)
    {
        var records = new List<Dictionary<string, string>>(); Dictionary<string, string>? current = null;
        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith("... ", StringComparison.Ordinal)) return [];
            int separator = line.IndexOf(' ', 4); if (separator <= 4) return [];
            string name = line[4..separator]; string value = separator == line.Length - 1 ? string.Empty : line[(separator + 1)..];
            if (current is null || name == "depotFile") { current = new(StringComparer.Ordinal); records.Add(current); }
            current[name] = value;
        }
        return records;
    }

    private static Dictionary<string, string> ParseInfo(string output)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith("... ", StringComparison.Ordinal)) continue;
            int separator = line.IndexOf(' ', 4);
            if (separator <= 4) continue;
            fields[line[4..separator]] = separator == line.Length - 1 ? string.Empty : line[(separator + 1)..];
        }
        return fields;
    }

    private static bool Required(Dictionary<string, string> record, string name, out string? value) => record.TryGetValue(name, out value) && !string.IsNullOrWhiteSpace(value);
    private static bool IsMalformedTagged(string output, List<Dictionary<string, string>> records) => !string.IsNullOrWhiteSpace(output) && records.Count == 0;
    private static bool IsWithinRoot(string path, string root) { string normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)); return path.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) || path.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase); }
    private static bool ContainsWildcard(string value) => value.IndexOfAny(['*', '?', '[', ']']) >= 0;
    private static bool IsExclusive(string type) => type.Split('+', 2).ElementAtOrDefault(1)?.Contains('l', StringComparison.Ordinal) == true;
    private static bool Truthy(Dictionary<string, string> record, string name) => record.TryGetValue(name, out string? value) && value is not "0" and not "false";
    private static bool ContainsAny(string value, params string[] candidates) => candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    private static P4FileOpenStatusResult Missing(string query, int limit) => P4FileOpenStatusResult.Failure(query, limit, P4FileOpenStatusErrorCode.MissingFile, "No matching file was found in the active workspace.");
    private static P4FileOpenStatusResult Malformed(string query, int limit) => P4FileOpenStatusResult.Failure(query, limit, P4FileOpenStatusErrorCode.MalformedOutput, "Perforce returned malformed machine-readable file status data.");
}
