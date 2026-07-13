namespace PerforceMcp.Perforce;

public sealed class P4OpenedFilesService
{
    public const int DefaultLimit = 50;
    public const int MaximumLimit = 200;

    private readonly IP4ProcessRunner _runner;

    public P4OpenedFilesService(P4ExecutableDiscoveryResult executable)
        : this(new P4ProcessRunner(executable))
    {
    }

    internal P4OpenedFilesService(IP4ProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<P4OpenedFilesResult> GetAsync(
        int limit = DefaultLimit,
        string? changelist = null,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > MaximumLimit)
        {
            return P4OpenedFilesResult.Failure(
                limit,
                P4OpenedFilesErrorCode.InvalidRequest,
                $"Limit must be between 1 and {MaximumLimit}.");
        }

        if (!TryNormalizeChangelist(changelist, out string? normalizedChangelist))
        {
            return P4OpenedFilesResult.Failure(
                limit,
                P4OpenedFilesErrorCode.InvalidRequest,
                "Changelist must be 'default' or a positive changelist number.");
        }

        var arguments = new List<string> { "-ztag", "opened", "-m", (limit + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) };
        if (normalizedChangelist is not null)
        {
            arguments.Add("-c");
            arguments.Add(normalizedChangelist);
        }

        P4ProcessResult processResult = await _runner
            .RunAsync(arguments, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (processResult.Error?.Code == P4ProcessErrorCode.TimedOut)
        {
            return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.TimedOut, "The Perforce server did not respond before the timeout.");
        }

        string commandOutput = string.Concat(processResult.StandardOutput, "\n", processResult.StandardError);
        if (!processResult.IsSuccess)
        {
            if (ContainsAny(commandOutput, "file(s) not opened", "no file(s) opened"))
            {
                return new P4OpenedFilesResult([], 0, limit, false, null);
            }

            return ClassifyCommandFailure(limit, commandOutput);
        }

        if (processResult.StandardOutputTruncated)
        {
            return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.MalformedOutput, "Perforce returned more machine-readable data than could be safely processed.");
        }

        return Parse(processResult.StandardOutput, limit);
    }

    internal static P4OpenedFilesResult Parse(string output, int limit)
    {
        ArgumentNullException.ThrowIfNull(output);
        var records = new List<Dictionary<string, string>>();
        Dictionary<string, string>? current = null;
        bool currentIsStream = false;
        bool sawNonEmptyLine = false;

        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            sawNonEmptyLine = true;
            if (!line.StartsWith("... ", StringComparison.Ordinal))
            {
                return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.MalformedOutput, "Perforce returned malformed machine-readable opened-file data.");
            }

            int valueSeparator = line.IndexOf(' ', 4);
            if (valueSeparator <= 4)
            {
                return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.MalformedOutput, "Perforce returned malformed machine-readable opened-file data.");
            }

            string name = line[4..valueSeparator];
            string value = valueSeparator == line.Length - 1 ? string.Empty : line[(valueSeparator + 1)..];
            if (name == "depotFile")
            {
                current = new Dictionary<string, string>(StringComparer.Ordinal);
                records.Add(current);
                currentIsStream = false;
            }
            else if (name == "stream")
            {
                current = new Dictionary<string, string>(StringComparer.Ordinal);
                currentIsStream = true;
            }

            if (current is null)
            {
                return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.MalformedOutput, "Perforce returned incomplete machine-readable opened-file data.");
            }

            if (!currentIsStream)
            {
                current[name] = value;
            }
        }

        if (!sawNonEmptyLine)
        {
            return new P4OpenedFilesResult([], 0, limit, false, null);
        }

        var files = new List<P4OpenedFile>();
        foreach (Dictionary<string, string> fields in records)
        {
            if (!TryRequired(fields, "depotFile", out string? depotPath) ||
                !TryRequired(fields, "action", out string? action) ||
                !TryRequired(fields, "change", out string? changelist) ||
                !TryRequired(fields, "type", out string? fileType))
            {
                return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.MalformedOutput, "Perforce returned incomplete machine-readable opened-file data.");
            }

            fields.TryGetValue("clientFile", out string? localPath);
            bool isLocked = HasTruthyTag(fields, "locked") || HasTruthyTag(fields, "isLocked") || HasTruthyTag(fields, "ourLock") || HasTruthyTag(fields, "lock");
            bool isExclusiveOpen = fileType!.Split('+', 2).ElementAtOrDefault(1)?.Contains('l', StringComparison.Ordinal) == true;
            files.Add(new P4OpenedFile(depotPath!, NullIfBlank(localPath), action!, changelist!, fileType, isLocked, isExclusiveOpen));
        }

        bool isTruncated = files.Count > limit;
        P4OpenedFile[] boundedFiles = files.Take(limit).ToArray();
        return new P4OpenedFilesResult(boundedFiles, boundedFiles.Length, limit, isTruncated, null);
    }

    private static bool TryNormalizeChangelist(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        string candidate = value.Trim();
        if (string.Equals(candidate, "default", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "default";
            return true;
        }

        if (int.TryParse(candidate, out int number) && number > 0)
        {
            normalized = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }

    private static P4OpenedFilesResult ClassifyCommandFailure(int limit, string output)
    {
        if (ContainsAny(output, "password invalid", "password (p4passwd) invalid", "please login", "your session has expired"))
        {
            return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.MissingLogin, "Perforce login is required or has expired.");
        }

        if (ContainsAny(output, "connect to server failed", "tcp connect", "partner exited unexpectedly"))
        {
            return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.UnreachableServer, "The configured Perforce server could not be reached.");
        }

        if (ContainsAny(output, "client unknown", "client '", "no client name specified"))
        {
            return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.MissingClient, "No valid Perforce client/workspace is configured for this environment.");
        }

        return P4OpenedFilesResult.Failure(limit, P4OpenedFilesErrorCode.CommandFailed, "Opened Perforce files could not be retrieved.");
    }

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

    private static bool HasTruthyTag(Dictionary<string, string> fields, string name) =>
        fields.TryGetValue(name, out string? value) &&
        !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
}
