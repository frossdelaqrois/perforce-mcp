using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace PerforceMcp.Perforce;

internal sealed partial class P4ProcessRunner
{
    internal const int MaximumCapturedCharactersPerStream = 32 * 1024;

    private static readonly TimeSpan ProcessCleanupTimeout = TimeSpan.FromSeconds(2);
    private readonly string _executablePath;

    public P4ProcessRunner(P4ExecutableDiscoveryResult executable)
    {
        ArgumentNullException.ThrowIfNull(executable);

        if (!executable.IsSuccess || executable.ExecutablePath is null)
        {
            throw new ArgumentException(
                "A successfully validated Perforce executable is required.",
                nameof(executable));
        }

        _executablePath = executable.ExecutablePath;
    }

    public async Task<P4ProcessResult> RunAsync(
        IReadOnlyList<string> arguments,
        P4ProcessRunnerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        options ??= new P4ProcessRunnerOptions();

        if (options.Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The process timeout must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        using var process = CreateProcess(arguments);

        try
        {
            if (!process.Start())
            {
                return StartFailed(stopwatch.Elapsed);
            }
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            return StartFailed(stopwatch.Elapsed);
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(options.Timeout);

        Task<CapturedOutput> standardOutput = ReadBoundedAsync(process.StandardOutput, timeoutSource.Token);
        Task<CapturedOutput> standardError = ReadBoundedAsync(process.StandardError, timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
            CapturedOutput[] output = await Task
                .WhenAll(standardOutput, standardError)
                .ConfigureAwait(false);
            stopwatch.Stop();

            string stdout = Redact(output[0].Text, options.SensitiveValues);
            string stderr = Redact(output[1].Text, options.SensitiveValues);
            P4ProcessError? error = process.ExitCode == 0
                ? null
                : new P4ProcessError(
                    P4ProcessErrorCode.NonZeroExit,
                    $"Perforce command exited with code {process.ExitCode}.");

            return new P4ProcessResult(
                process.ExitCode,
                stdout,
                stderr,
                output[0].IsTruncated,
                output[1].IsTruncated,
                stopwatch.Elapsed,
                error);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await StopProcessAsync(process).ConfigureAwait(false);
            await IgnoreOutputAsync(standardOutput, standardError).ConfigureAwait(false);
            stopwatch.Stop();
            return new P4ProcessResult(
                null,
                string.Empty,
                string.Empty,
                false,
                false,
                stopwatch.Elapsed,
                new P4ProcessError(P4ProcessErrorCode.TimedOut, "Perforce command timed out."));
        }
        catch (OperationCanceledException)
        {
            await StopProcessAsync(process).ConfigureAwait(false);
            await IgnoreOutputAsync(standardOutput, standardError).ConfigureAwait(false);
            throw;
        }
    }

    private Process CreateProcess(IReadOnlyList<string> arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };

        foreach (string argument in arguments)
        {
            ArgumentNullException.ThrowIfNull(argument);
            process.StartInfo.ArgumentList.Add(argument);
        }

        return process;
    }

    private static P4ProcessResult StartFailed(TimeSpan duration) =>
        new(
            null,
            string.Empty,
            string.Empty,
            false,
            false,
            duration,
            new P4ProcessError(
                P4ProcessErrorCode.StartFailed,
                "Perforce command could not be started. Check the validated executable path and file permissions."));

    private static async Task<CapturedOutput> ReadBoundedAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var captured = new StringBuilder(MaximumCapturedCharactersPerStream);
        var buffer = new char[4096];
        bool isTruncated = false;

        while (true)
        {
            int charactersRead = await reader
                .ReadAsync(buffer.AsMemory(), cancellationToken)
                .ConfigureAwait(false);
            if (charactersRead == 0)
            {
                break;
            }

            int remainingCapacity = MaximumCapturedCharactersPerStream - captured.Length;
            if (remainingCapacity > 0)
            {
                captured.Append(buffer, 0, Math.Min(charactersRead, remainingCapacity));
            }

            isTruncated |= charactersRead > remainingCapacity;
        }

        return new CapturedOutput(captured.ToString(), isTruncated);
    }

    private static string Redact(string value, IReadOnlyCollection<string> sensitiveValues)
    {
        string redacted = SensitiveAssignmentRegex().Replace(value, "$1[REDACTED]");

        foreach (string sensitiveValue in sensitiveValues)
        {
            if (!string.IsNullOrEmpty(sensitiveValue))
            {
                redacted = redacted.Replace(sensitiveValue, "[REDACTED]", StringComparison.Ordinal);
            }
        }

        return redacted;
    }

    private static async Task StopProcessAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
        }

        using var cleanupSource = new CancellationTokenSource(ProcessCleanupTimeout);

        try
        {
            await process.WaitForExitAsync(cleanupSource.Token).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }
        catch (OperationCanceledException) when (cleanupSource.IsCancellationRequested)
        {
        }
    }

    private static async Task IgnoreOutputAsync(params Task<CapturedOutput>[] outputTasks)
    {
        try
        {
            await Task.WhenAll(outputTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
    }

    [GeneratedRegex(
        "(?i)(\\b(?:P4PASSWD|password|passwd|ticket)\\s*[:=]\\s*)([^\\s;]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveAssignmentRegex();

    private readonly record struct CapturedOutput(string Text, bool IsTruncated);
}
