using System.Globalization;

namespace PerforceMcp.Perforce;

public sealed class P4PendingChangelistsService
{
    public const int DefaultLimit = 20;
    public const int MaximumLimit = 100;
    public const int DefaultFileLimit = 100;
    public const int MaximumFileLimit = 200;

    private readonly IP4ProcessRunner _runner;

    public P4PendingChangelistsService(P4ExecutableDiscoveryResult executable)
        : this(new P4ProcessRunner(executable))
    {
    }

    internal P4PendingChangelistsService(IP4ProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<P4PendingChangelistsResult> GetAsync(
        int limit = DefaultLimit,
        bool includeFiles = false,
        int fileLimit = DefaultFileLimit,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > MaximumLimit || fileLimit is < 1 or > MaximumFileLimit)
        {
            return Failure(
                P4PendingChangelistsErrorCode.InvalidRequest,
                $"Limit must be between 1 and {MaximumLimit}, and fileLimit must be between 1 and {MaximumFileLimit}.");
        }

        P4InfoResult info = await new P4InfoService(_runner).GetAsync(cancellationToken).ConfigureAwait(false);
        if (!info.IsSuccess)
        {
            P4PendingChangelistsError mapped = MapInfoFailure(info.Error!);
            return Failure(mapped.Code, mapped.Message);
        }

        IReadOnlyList<string> changeArguments =
        [
            "-ztag", "changes", "-s", "pending", "-u", info.User!, "-c", info.Client!,
            "-t", "-L", "-m", (limit + 1).ToString(CultureInfo.InvariantCulture),
        ];
        P4ProcessResult changesProcess = await _runner.RunAsync(changeArguments, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (changesProcess.Error?.Code == P4ProcessErrorCode.TimedOut)
        {
            return Failure(P4PendingChangelistsErrorCode.TimedOut, "The Perforce server did not respond before the timeout.");
        }

        if (!changesProcess.IsSuccess)
        {
            P4PendingChangelistsError classified = ClassifyCommandFailure(string.Concat(changesProcess.StandardOutput, "\n", changesProcess.StandardError));
            return Failure(classified.Code, classified.Message);
        }

        if (changesProcess.StandardOutputTruncated)
        {
            return Failure(P4PendingChangelistsErrorCode.MalformedOutput, "Perforce returned more machine-readable data than could be safely processed.");
        }

        ParseChangesResult parsedChanges = ParseChanges(changesProcess.StandardOutput);
        if (parsedChanges.Error is not null)
        {
            return Failure(P4PendingChangelistsErrorCode.MalformedOutput, parsedChanges.Error);
        }

        var results = new List<P4PendingChangelist>();
        OpenedLookup defaultFiles = await GetOpenedAsync("default", fileLimit, cancellationToken).ConfigureAwait(false);
        if (defaultFiles.Error is not null)
        {
            return Failure(defaultFiles.Error.Code, defaultFiles.Error.Message);
        }

        if (defaultFiles.Files.Count > 0)
        {
            results.Add(CreateDefault(info, defaultFiles, includeFiles));
        }

        foreach (ParsedChange change in parsedChanges.Changes)
        {
            OpenedLookup opened = await GetOpenedAsync(change.Number.ToString(CultureInfo.InvariantCulture), fileLimit, cancellationToken).ConfigureAwait(false);
            if (opened.Error is not null)
            {
                return Failure(opened.Error.Code, opened.Error.Message);
            }

            results.Add(CreateNumbered(change, opened, includeFiles));
        }

        bool isTruncated = results.Count > limit || parsedChanges.Changes.Count > limit;
        P4PendingChangelist[] bounded = results.Take(limit).ToArray();
        return new P4PendingChangelistsResult(bounded, bounded.Length, limit, includeFiles, fileLimit, isTruncated, null);

        P4PendingChangelistsResult Failure(P4PendingChangelistsErrorCode code, string message) =>
            P4PendingChangelistsResult.Failure(limit, includeFiles, fileLimit, code, message);
    }

    internal static ParseChangesResult ParseChanges(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (string.IsNullOrWhiteSpace(output))
        {
            return new ParseChangesResult([], null);
        }

        var records = new List<Dictionary<string, string>>();
        Dictionary<string, string>? current = null;
        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith("... ", StringComparison.Ordinal))
            {
                return new ParseChangesResult([], "Perforce returned malformed machine-readable changelist data.");
            }

            int separator = line.IndexOf(' ', 4);
            if (separator <= 4)
            {
                return new ParseChangesResult([], "Perforce returned malformed machine-readable changelist data.");
            }

            string name = line[4..separator];
            string value = separator == line.Length - 1 ? string.Empty : line[(separator + 1)..];
            if (name == "change")
            {
                current = new Dictionary<string, string>(StringComparer.Ordinal);
                records.Add(current);
            }

            if (current is null)
            {
                return new ParseChangesResult([], "Perforce returned incomplete machine-readable changelist data.");
            }

            current[name] = value;
        }

        var changes = new List<ParsedChange>();
        foreach (Dictionary<string, string> fields in records)
        {
            if (!TryRequired(fields, "change", out string? changeText) ||
                !int.TryParse(changeText, NumberStyles.None, CultureInfo.InvariantCulture, out int number) || number <= 0 ||
                !TryRequired(fields, "user", out string? owner) ||
                !TryRequired(fields, "client", out string? client) ||
                !TryRequired(fields, "status", out string? status) ||
                !TryRequired(fields, "desc", out string? description) ||
                !TryRequired(fields, "time", out string? timeText) ||
                !long.TryParse(timeText, NumberStyles.None, CultureInfo.InvariantCulture, out long unixTime))
            {
                return new ParseChangesResult([], "Perforce returned incomplete machine-readable changelist data.");
            }

            DateTimeOffset modifiedTime;
            try
            {
                modifiedTime = DateTimeOffset.FromUnixTimeSeconds(unixTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                return new ParseChangesResult([], "Perforce returned invalid machine-readable changelist data.");
            }

            changes.Add(new ParsedChange(number, description!, owner!, client!, status!, modifiedTime));
        }

        return new ParseChangesResult(changes, null);
    }

    private async Task<OpenedLookup> GetOpenedAsync(string changelist, int fileLimit, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> arguments = ["-ztag", "opened", "-m", (fileLimit + 1).ToString(CultureInfo.InvariantCulture), "-c", changelist];
        P4ProcessResult process = await _runner.RunAsync(arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (process.Error?.Code == P4ProcessErrorCode.TimedOut)
        {
            return OpenedLookup.Failed(new P4PendingChangelistsError(P4PendingChangelistsErrorCode.TimedOut, "The Perforce server did not respond before the timeout."));
        }

        string output = string.Concat(process.StandardOutput, "\n", process.StandardError);
        if (!process.IsSuccess)
        {
            if (ContainsAny(output, "file(s) not opened", "no file(s) opened", "unknown changelist"))
            {
                return new OpenedLookup([], false, null);
            }

            return OpenedLookup.Failed(ClassifyCommandFailure(output));
        }

        if (process.StandardOutputTruncated)
        {
            return OpenedLookup.Failed(new P4PendingChangelistsError(P4PendingChangelistsErrorCode.MalformedOutput, "Perforce returned more machine-readable data than could be safely processed."));
        }

        P4OpenedFilesResult parsed = P4OpenedFilesService.Parse(process.StandardOutput, fileLimit);
        if (!parsed.IsSuccess)
        {
            return OpenedLookup.Failed(new P4PendingChangelistsError(P4PendingChangelistsErrorCode.MalformedOutput, "Perforce returned malformed machine-readable opened-file data."));
        }

        P4PendingFile[] files = parsed.Files.Select(file => new P4PendingFile(file.DepotPath, file.LocalPath, file.Action, file.FileType)).ToArray();
        return new OpenedLookup(files, parsed.IsTruncated, null);
    }

    private static P4PendingChangelistsError MapInfoFailure(P4InfoError error) => error.Code switch
    {
        P4InfoErrorCode.MissingLogin => new(P4PendingChangelistsErrorCode.MissingLogin, "Perforce login is required or has expired."),
        P4InfoErrorCode.MissingClient => new(P4PendingChangelistsErrorCode.MissingClient, "No valid Perforce client/workspace is configured for this environment."),
        P4InfoErrorCode.UnreachableServer => new(P4PendingChangelistsErrorCode.UnreachableServer, "The configured Perforce server could not be reached."),
        P4InfoErrorCode.TimedOut => new(P4PendingChangelistsErrorCode.TimedOut, "The Perforce server did not respond before the timeout."),
        P4InfoErrorCode.MalformedOutput => new(P4PendingChangelistsErrorCode.MalformedOutput, "Perforce returned incomplete machine-readable information."),
        _ => new(P4PendingChangelistsErrorCode.CommandFailed, "Pending Perforce changelists could not be retrieved."),
    };

    private static P4PendingChangelistsError ClassifyCommandFailure(string output)
    {
        if (ContainsAny(output, "password invalid", "password (p4passwd) invalid", "please login", "your session has expired"))
        {
            return new(P4PendingChangelistsErrorCode.MissingLogin, "Perforce login is required or has expired.");
        }

        if (ContainsAny(output, "connect to server failed", "tcp connect", "partner exited unexpectedly"))
        {
            return new(P4PendingChangelistsErrorCode.UnreachableServer, "The configured Perforce server could not be reached.");
        }

        if (ContainsAny(output, "client unknown", "client '", "no client name specified"))
        {
            return new(P4PendingChangelistsErrorCode.MissingClient, "No valid Perforce client/workspace is configured for this environment.");
        }

        return new(P4PendingChangelistsErrorCode.CommandFailed, "Pending Perforce changelists could not be retrieved.");
    }

    private static P4PendingChangelist CreateDefault(P4InfoResult info, OpenedLookup opened, bool includeFiles) =>
        new("default", null, true, "Default changelist", info.User!, info.Client!, "pending", null, opened.Files.Count, !opened.IsTruncated, includeFiles ? opened.Files : [], opened.IsTruncated);

    private static P4PendingChangelist CreateNumbered(ParsedChange change, OpenedLookup opened, bool includeFiles) =>
        new(change.Number.ToString(CultureInfo.InvariantCulture), change.Number, false, change.Description, change.Owner, change.Client, change.Status, change.ModifiedTime, opened.Files.Count, !opened.IsTruncated, includeFiles ? opened.Files : [], opened.IsTruncated);

    private static bool TryRequired(Dictionary<string, string> fields, string name, out string? value)
    {
        if (fields.TryGetValue(name, out string? candidate) && !string.IsNullOrWhiteSpace(candidate))
        {
            value = candidate;
            return true;
        }

        value = null;
        return false;
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    internal sealed record ParseChangesResult(IReadOnlyList<ParsedChange> Changes, string? Error);
    internal sealed record ParsedChange(int Number, string Description, string Owner, string Client, string Status, DateTimeOffset ModifiedTime);
    private sealed record OpenedLookup(IReadOnlyList<P4PendingFile> Files, bool IsTruncated, P4PendingChangelistsError? Error)
    {
        public static OpenedLookup Failed(P4PendingChangelistsError error) => new([], false, error);
    }
}
