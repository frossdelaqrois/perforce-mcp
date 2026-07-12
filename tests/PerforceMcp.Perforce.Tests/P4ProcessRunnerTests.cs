using System.Diagnostics;

namespace PerforceMcp.Perforce.Tests;

[Collection(P4ProcessTestGroup.Name)]
public sealed class P4ProcessRunnerTests
{
    [Fact]
    public async Task ReturnsStructuredSuccess()
    {
        P4ProcessResult result = await CreateRunner().RunAsync(["success"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("success", result.StandardOutput);
        Assert.Equal("diagnostic", result.StandardError);
        Assert.Null(result.Error);
        Assert.True(result.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task PreservesArgumentsWithoutShellInterpretation()
    {
        P4ProcessResult result = await CreateRunner().RunAsync(
            ["arguments", "value with spaces", "& echo unsafe"]);

        Assert.Equal("value with spaces\n& echo unsafe", NormalizeLineEndings(result.StandardOutput));
    }

    [Fact]
    public async Task ReturnsStructuredErrorForNonZeroExitAndRedactsSecrets()
    {
        var options = new P4ProcessRunnerOptions
        {
            SensitiveValues = ["known-secret"],
        };

        P4ProcessResult result = await CreateRunner().RunAsync(["failure"], options);

        Assert.False(result.IsSuccess);
        Assert.Equal(7, result.ExitCode);
        Assert.Equal(P4ProcessErrorCode.NonZeroExit, result.Error?.Code);
        Assert.DoesNotContain("hunter2", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain("ABC123", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain("known-secret", result.StandardError, StringComparison.Ordinal);
        Assert.Equal(3, CountOccurrences(result.StandardError, "[REDACTED]"));
    }

    [Fact]
    public async Task TimeoutTerminatesTheChildProcess()
    {
        HashSet<int> existingProcessIds = GetTestProcessIds();
        var options = new P4ProcessRunnerOptions
        {
            Timeout = TimeSpan.FromMilliseconds(250),
        };

        P4ProcessResult result = await CreateRunner().RunAsync(["wait"], options);

        Assert.Equal(P4ProcessErrorCode.TimedOut, result.Error?.Code);
        Assert.Null(result.ExitCode);
        Assert.Empty(GetTestProcessIds().Except(existingProcessIds));
    }

    [Fact]
    public async Task CancellationTerminatesTheChildProcessAndPropagates()
    {
        HashSet<int> existingProcessIds = GetTestProcessIds();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateRunner().RunAsync(
                ["wait"],
                new P4ProcessRunnerOptions { Timeout = TimeSpan.FromSeconds(10) },
                cancellationSource.Token));

        Assert.Empty(GetTestProcessIds().Except(existingProcessIds));
    }

    [Fact]
    public async Task CapsBothOutputStreams()
    {
        P4ProcessResult result = await CreateRunner().RunAsync(["oversized-output"]);

        Assert.Equal(P4ProcessRunner.MaximumCapturedCharactersPerStream, result.StandardOutput.Length);
        Assert.Equal(P4ProcessRunner.MaximumCapturedCharactersPerStream, result.StandardError.Length);
        Assert.True(result.StandardOutputTruncated);
        Assert.True(result.StandardErrorTruncated);
    }

    [Fact]
    public async Task ReturnsStructuredErrorWhenProcessCannotStart()
    {
        string missingPath = Path.Combine(
            Path.GetTempPath(),
            "perforce-mcp-tests",
            "missing",
            OperatingSystem.IsWindows() ? "p4.exe" : "p4");
        var runner = new P4ProcessRunner(
            P4ExecutableDiscoveryResult.Success(missingPath, "test version"));

        P4ProcessResult result = await runner.RunAsync(["info"]);

        Assert.Equal(P4ProcessErrorCode.StartFailed, result.Error?.Code);
        Assert.Null(result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Empty(result.StandardError);
    }

    [Fact]
    public void RejectsUnvalidatedExecutableResult()
    {
        P4ExecutableDiscoveryResult failed = P4ExecutableDiscoveryResult.Failure(
            P4ExecutableDiscoveryErrorCode.NotFound,
            "not found");

        Assert.Throws<ArgumentException>(() => new P4ProcessRunner(failed));
    }

    private static P4ProcessRunner CreateRunner()
    {
        string executablePath = Path.Combine(
            AppContext.BaseDirectory,
            OperatingSystem.IsWindows()
                ? "P4ValidatorTestProcess.exe"
                : "P4ValidatorTestProcess");
        return new P4ProcessRunner(
            P4ExecutableDiscoveryResult.Success(executablePath, "test version"));
    }

    private static HashSet<int> GetTestProcessIds()
    {
        string processName = OperatingSystem.IsWindows()
            ? "P4ValidatorTestProcess"
            : "P4ValidatorTest";
        return Process
            .GetProcessesByName(processName)
            .Select(process =>
            {
                using (process)
                {
                    return process.Id;
                }
            })
            .ToHashSet();
    }

    private static int CountOccurrences(string value, string searchValue) =>
        value.Split(searchValue, StringSplitOptions.None).Length - 1;

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
