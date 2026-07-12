namespace PerforceMcp.Perforce;

public sealed class P4ExecutableDiscovery
{
    private const string MissingExecutableMessage =
        "Perforce command-line client was not found. Configure an absolute path to p4 or p4.exe, or add it to PATH.";

    private readonly IP4ExecutablePlatform _platform;
    private readonly IP4ExecutableValidator _validator;

    public P4ExecutableDiscovery()
        : this(new SystemP4ExecutablePlatform(), new P4ExecutableValidator())
    {
    }

    internal P4ExecutableDiscovery(
        IP4ExecutablePlatform platform,
        IP4ExecutableValidator validator)
    {
        _platform = platform;
        _validator = validator;
    }

    public async Task<P4ExecutableDiscoveryResult> DiscoverAsync(
        P4ExecutableDiscoveryOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (options.ValidationTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The validation timeout must be greater than zero.");
        }

        if (!string.IsNullOrWhiteSpace(options.ExecutablePath))
        {
            return await DiscoverConfiguredAsync(options, cancellationToken).ConfigureAwait(false);
        }

        return await DiscoverFromPathAsync(options.ValidationTimeout, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<P4ExecutableDiscoveryResult> DiscoverConfiguredAsync(
        P4ExecutableDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        if (!_platform.IsPathFullyQualified(options.ExecutablePath!))
        {
            return P4ExecutableDiscoveryResult.Failure(
                P4ExecutableDiscoveryErrorCode.InvalidExecutable,
                "The configured Perforce executable path must be absolute.");
        }

        string executablePath;
        try
        {
            executablePath = _platform.GetFullPath(options.ExecutablePath!);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return P4ExecutableDiscoveryResult.Failure(
                P4ExecutableDiscoveryErrorCode.InvalidExecutable,
                "The configured Perforce executable path is invalid.");
        }

        if (!HasExpectedFileName(executablePath))
        {
            return P4ExecutableDiscoveryResult.Failure(
                P4ExecutableDiscoveryErrorCode.InvalidExecutable,
                "The configured executable must be named p4 or p4.exe.");
        }

        if (!_platform.FileExists(executablePath))
        {
            return P4ExecutableDiscoveryResult.Failure(
                P4ExecutableDiscoveryErrorCode.NotFound,
                "The configured Perforce executable does not exist. Set it to an existing p4 or p4.exe file.");
        }

        return await ValidateAsync(executablePath, options.ValidationTimeout, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<P4ExecutableDiscoveryResult> DiscoverFromPathAsync(
        TimeSpan validationTimeout,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_platform.PathValue))
        {
            return P4ExecutableDiscoveryResult.Failure(
                P4ExecutableDiscoveryErrorCode.NotFound,
                MissingExecutableMessage);
        }

        P4ExecutableDiscoveryResult? lastValidationFailure = null;
        var visitedCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string entry in _platform.PathValue.Split(_platform.PathSeparator))
        {
            string directory = entry.Trim().Trim('"');
            if (directory.Length == 0 || !_platform.IsPathFullyQualified(directory))
            {
                continue;
            }

            string candidate;
            try
            {
                candidate = _platform.GetFullPath(
                    _platform.Combine(directory, _platform.ExecutableFileName));
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (!visitedCandidates.Add(candidate) || !_platform.FileExists(candidate))
            {
                continue;
            }

            P4ExecutableDiscoveryResult validationResult =
                await ValidateAsync(candidate, validationTimeout, cancellationToken)
                    .ConfigureAwait(false);

            if (validationResult.IsSuccess)
            {
                return validationResult;
            }

            lastValidationFailure = validationResult;
        }

        return lastValidationFailure ?? P4ExecutableDiscoveryResult.Failure(
            P4ExecutableDiscoveryErrorCode.NotFound,
            MissingExecutableMessage);
    }

    private async Task<P4ExecutableDiscoveryResult> ValidateAsync(
        string executablePath,
        TimeSpan validationTimeout,
        CancellationToken cancellationToken)
    {
        P4ExecutableValidationResult validation = await _validator
            .ValidateAsync(executablePath, validationTimeout, cancellationToken)
            .ConfigureAwait(false);

        return validation.Status switch
        {
            P4ExecutableValidationStatus.Valid =>
                P4ExecutableDiscoveryResult.Success(
                    executablePath,
                    validation.Version ?? throw new InvalidOperationException(
                        "A valid Perforce executable must include a version.")),
            P4ExecutableValidationStatus.Invalid =>
                P4ExecutableDiscoveryResult.Failure(
                    P4ExecutableDiscoveryErrorCode.ValidationFailed,
                    "The executable did not identify itself as the Perforce command-line client when run with -V."),
            P4ExecutableValidationStatus.TimedOut =>
                P4ExecutableDiscoveryResult.Failure(
                    P4ExecutableDiscoveryErrorCode.ValidationTimedOut,
                    "Perforce executable validation timed out. Check the configured executable and try again."),
            P4ExecutableValidationStatus.CouldNotStart =>
                P4ExecutableDiscoveryResult.Failure(
                    P4ExecutableDiscoveryErrorCode.StartFailed,
                    "The Perforce executable could not be started. Check file permissions and the configured path."),
            _ => throw new InvalidOperationException("Unknown Perforce executable validation status."),
        };
    }

    private static bool HasExpectedFileName(string executablePath)
    {
        string fileName = Path.GetFileName(executablePath);
        return string.Equals(fileName, "p4", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "p4.exe", StringComparison.OrdinalIgnoreCase);
    }
}
