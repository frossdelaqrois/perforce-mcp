namespace PerforceMcp.Perforce;

public sealed class P4InfoService
{
    private static readonly string[] InfoArguments = ["-ztag", "info"];
    private readonly IP4ProcessRunner _runner;

    public P4InfoService(P4ExecutableDiscoveryResult executable)
        : this(new P4ProcessRunner(executable))
    {
    }

    internal P4InfoService(IP4ProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<P4InfoResult> GetAsync(CancellationToken cancellationToken = default)
    {
        P4ProcessResult processResult = await _runner
            .RunAsync(InfoArguments, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (processResult.Error?.Code == P4ProcessErrorCode.TimedOut)
        {
            return P4InfoResult.Failure(P4InfoErrorCode.TimedOut, "The Perforce server did not respond before the timeout.");
        }

        if (!processResult.IsSuccess)
        {
            return ClassifyCommandFailure(
                string.Concat(processResult.StandardOutput, "\n", processResult.StandardError));
        }

        return Parse(processResult.StandardOutput);
    }

    internal static P4InfoResult Parse(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith("... ", StringComparison.Ordinal))
            {
                continue;
            }

            int valueSeparator = line.IndexOf(' ', 4);
            if (valueSeparator <= 4 || valueSeparator == line.Length - 1)
            {
                continue;
            }

            fields[line[4..valueSeparator]] = line[(valueSeparator + 1)..];
        }

        if (!TryGetRequired(fields, "serverAddress", out string? serverAddress) ||
            !TryGetRequired(fields, "userName", out string? user) ||
            !TryGetRequired(fields, "serverVersion", out string? serverVersion))
        {
            return P4InfoResult.Failure(
                P4InfoErrorCode.MalformedOutput,
                "Perforce returned incomplete machine-readable information.");
        }

        if (!TryGetRequired(fields, "clientName", out string? client) ||
            IsUnknownClient(client!) ||
            !TryGetRequired(fields, "clientRoot", out string? clientRoot))
        {
            return new P4InfoResult(
                serverAddress,
                user,
                null,
                null,
                serverVersion,
                new P4InfoError(
                    P4InfoErrorCode.MissingClient,
                    "No valid Perforce client/workspace is configured for this environment."));
        }

        return new P4InfoResult(serverAddress, user, client, clientRoot, serverVersion, null);
    }

    private static P4InfoResult ClassifyCommandFailure(string standardError)
    {
        if (ContainsAny(standardError, "password invalid", "password (p4passwd) invalid", "please login", "your session has expired"))
        {
            return P4InfoResult.Failure(P4InfoErrorCode.MissingLogin, "Perforce login is required or has expired.");
        }

        if (ContainsAny(standardError, "connect to server failed", "tcp connect", "partner exited unexpectedly"))
        {
            return P4InfoResult.Failure(P4InfoErrorCode.UnreachableServer, "The configured Perforce server could not be reached.");
        }

        if (ContainsAny(standardError, "client unknown", "client '"))
        {
            return P4InfoResult.Failure(P4InfoErrorCode.MissingClient, "No valid Perforce client/workspace is configured for this environment.");
        }

        return P4InfoResult.Failure(P4InfoErrorCode.CommandFailed, "Perforce information could not be retrieved.");
    }

    private static bool TryGetRequired(
        Dictionary<string, string> fields,
        string name,
        out string? value)
    {
        if (fields.TryGetValue(name, out string? candidate) && !string.IsNullOrWhiteSpace(candidate))
        {
            value = candidate;
            return true;
        }

        value = null;
        return false;
    }

    private static bool IsUnknownClient(string client) =>
        string.Equals(client, "*unknown*", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(client, "unknown", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
}
