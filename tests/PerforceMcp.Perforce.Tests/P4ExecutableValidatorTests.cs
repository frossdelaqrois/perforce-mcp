using System.Diagnostics;

namespace PerforceMcp.Perforce.Tests;

[Collection(P4ProcessTestGroup.Name)]
public sealed class P4ExecutableValidatorTests
{
    [Fact]
    public async Task TimeoutTerminatesTheChildProcess()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string executablePath = Path.Combine(
            AppContext.BaseDirectory,
            "P4ValidatorTestProcess.exe");
        var validator = new P4ExecutableValidator();
        HashSet<int> existingProcessIds = GetTestProcessIds();

        P4ExecutableValidationResult result = await validator.ValidateAsync(
            executablePath,
            TimeSpan.FromMilliseconds(250),
            CancellationToken.None);

        Assert.Equal(P4ExecutableValidationStatus.TimedOut, result.Status);
        Assert.Empty(GetTestProcessIds().Except(existingProcessIds));
    }

    [Fact]
    public void RecognizesPerforceVersionOutput()
    {
        const string Output = """
            Perforce - The Fast Software Configuration Management System.
            Rev. P4/NTX64/2025.1/1234567 (2025/01/01).
            """;

        Assert.Equal(
            "Rev. P4/NTX64/2025.1/1234567 (2025/01/01).",
            P4ExecutableValidator.ParsePerforceVersion(Output));
    }

    [Theory]
    [InlineData("")]
    [InlineData("git version 2.50.0")]
    [InlineData("Perforce - The Fast Software Configuration Management System.")]
    public void RejectsNonPerforceVersionOutput(string output)
    {
        Assert.Null(P4ExecutableValidator.ParsePerforceVersion(output));
    }

    private static HashSet<int> GetTestProcessIds()
    {
        using Process currentProcess = Process.GetCurrentProcess();
        return Process
            .GetProcessesByName("P4ValidatorTestProcess")
            .Where(process => process.Id != currentProcess.Id)
            .Select(process =>
            {
                using (process)
                {
                    return process.Id;
                }
            })
            .ToHashSet();
    }
}
