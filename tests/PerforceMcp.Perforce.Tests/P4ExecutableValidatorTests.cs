namespace PerforceMcp.Perforce.Tests;

public sealed class P4ExecutableValidatorTests
{
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
}
