using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace PerforceMcp.Perforce;

internal enum P4ExecutableValidationStatus
{
    Valid,
    Invalid,
    TimedOut,
    CouldNotStart,
}

internal readonly record struct P4ExecutableValidationResult(
    P4ExecutableValidationStatus Status,
    string? Version = null);

internal interface IP4ExecutableValidator
{
    Task<P4ExecutableValidationResult> ValidateAsync(
        string executablePath,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

internal sealed class P4ExecutableValidator : IP4ExecutableValidator
{
    private const int MaximumCapturedCharactersPerStream = 32 * 1024;
    private static readonly TimeSpan ProcessCleanupTimeout = TimeSpan.FromSeconds(2);

    public async Task<P4ExecutableValidationResult> ValidateAsync(
        string executablePath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };
        process.StartInfo.ArgumentList.Add("-V");

        try
        {
            if (!process.Start())
            {
                return new(P4ExecutableValidationStatus.CouldNotStart);
            }
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            return new(P4ExecutableValidationStatus.CouldNotStart);
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        Task<string> standardOutput = ReadBoundedAsync(
            process.StandardOutput,
            timeoutSource.Token);
        Task<string> standardError = ReadBoundedAsync(
            process.StandardError,
            timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
            string[] output = await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);

            string? version = ParsePerforceVersion(string.Concat(output));
            return version is not null
                ? new P4ExecutableValidationResult(P4ExecutableValidationStatus.Valid, version)
                : new P4ExecutableValidationResult(P4ExecutableValidationStatus.Invalid);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await StopProcessAsync(process).ConfigureAwait(false);
            await IgnoreOutputAsync(standardOutput, standardError).ConfigureAwait(false);
            return new(P4ExecutableValidationStatus.TimedOut);
        }
        catch (OperationCanceledException)
        {
            await StopProcessAsync(process).ConfigureAwait(false);
            await IgnoreOutputAsync(standardOutput, standardError).ConfigureAwait(false);
            throw;
        }
    }

    internal static string? ParsePerforceVersion(string output)
    {
        if (!output.Contains(
            "Perforce - The Fast Software Configuration Management System",
            StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (ReadOnlySpan<char> line in output.AsSpan().EnumerateLines())
        {
            ReadOnlySpan<char> trimmedLine = line.Trim();
            if (trimmedLine.Length <= 256 &&
                trimmedLine.StartsWith("Rev. P4/", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedLine.ToString();
            }
        }

        return null;
    }

    private static async Task<string> ReadBoundedAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var captured = new StringBuilder(MaximumCapturedCharactersPerStream);
        var buffer = new char[4096];

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
        }

        return captured.ToString();
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
            await process
                .WaitForExitAsync(cleanupSource.Token)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }
        catch (OperationCanceledException) when (cleanupSource.IsCancellationRequested)
        {
        }
    }

    private static async Task IgnoreOutputAsync(params Task<string>[] outputTasks)
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
}
