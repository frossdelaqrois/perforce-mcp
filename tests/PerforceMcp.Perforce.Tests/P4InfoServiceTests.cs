namespace PerforceMcp.Perforce.Tests;

public sealed class P4InfoServiceTests
{
    private const string FictionalTaggedInfo = """
        ... userName avery.dev
        ... clientName aurora-main-ws
        ... clientRoot D:\FictionalStudio\Aurora
        ... serverAddress perforce.example.test:1666
        ... serverVersion P4D/FICTIONAL/2025.1/1234567 (2025/01/15)
        """;

    [Fact]
    public async Task RunsOnlyTaggedInfoAndReturnsStructuredFields()
    {
        var runner = new FakeRunner(Success(FictionalTaggedInfo));
        var service = new P4InfoService(runner);

        P4InfoResult result = await service.GetAsync();

        Assert.Equal(["-ztag", "info"], runner.Arguments);
        Assert.True(result.IsSuccess);
        Assert.Equal("perforce.example.test:1666", result.ServerAddress);
        Assert.Equal("avery.dev", result.User);
        Assert.Equal("aurora-main-ws", result.Client);
        Assert.Equal("D:\\FictionalStudio\\Aurora", result.ClientRoot);
        Assert.Equal("P4D/FICTIONAL/2025.1/1234567 (2025/01/15)", result.ServerVersion);
    }

    [Fact]
    public async Task ReportsMissingClientWhilePreservingSafeConnectionFields()
    {
        const string fixture = """
            ... userName avery.dev
            ... clientName *unknown*
            ... serverAddress perforce.example.test:1666
            ... serverVersion P4D/FICTIONAL/2025.1/1234567 (2025/01/15)
            """;
        var service = new P4InfoService(new FakeRunner(Success(fixture)));

        P4InfoResult result = await service.GetAsync();

        Assert.Equal(P4InfoErrorCode.MissingClient, result.Error?.Code);
        Assert.Equal("perforce.example.test:1666", result.ServerAddress);
        Assert.Equal("avery.dev", result.User);
        Assert.Null(result.Client);
        Assert.Null(result.ClientRoot);
    }

    [Theory]
    [InlineData("Perforce password (P4PASSWD) invalid or unset.", P4InfoErrorCode.MissingLogin)]
    [InlineData("Connect to server failed; check $P4PORT.", P4InfoErrorCode.UnreachableServer)]
    [InlineData("Client 'missing-workspace' unknown - use 'client' command to create it.", P4InfoErrorCode.MissingClient)]
    public async Task ClassifiesExpectedCommandFailures(string standardError, P4InfoErrorCode expected)
    {
        var service = new P4InfoService(new FakeRunner(Failure(standardError)));

        P4InfoResult result = await service.GetAsync();

        Assert.Equal(expected, result.Error?.Code);
        Assert.Null(result.ServerAddress);
        Assert.DoesNotContain("P4PASSWD", result.Error?.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("missing-workspace", result.Error?.Message ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsMalformedTaggedOutput()
    {
        var service = new P4InfoService(new FakeRunner(Success("... userName avery.dev\n... unexpected value")));

        P4InfoResult result = await service.GetAsync();

        Assert.Equal(P4InfoErrorCode.MalformedOutput, result.Error?.Code);
    }

    [Fact]
    public async Task ReportsTimeoutWithoutReturningCapturedOutput()
    {
        var service = new P4InfoService(new FakeRunner(new P4ProcessResult(
            null,
            "ticket=FICTIONAL_SECRET",
            string.Empty,
            false,
            false,
            TimeSpan.FromSeconds(30),
            new P4ProcessError(P4ProcessErrorCode.TimedOut, "timed out"))));

        P4InfoResult result = await service.GetAsync();

        Assert.Equal(P4InfoErrorCode.TimedOut, result.Error?.Code);
        Assert.Null(result.ServerAddress);
        Assert.DoesNotContain("FICTIONAL_SECRET", result.Error?.Message ?? string.Empty, StringComparison.Ordinal);
    }

    [P4IntegrationFact]
    public async Task OptionalIntegrationTest()
    {
        string executablePath = Environment.GetEnvironmentVariable("PERFORCE_MCP_TEST_P4_PATH")!;

        P4ExecutableDiscoveryResult discovery = await new P4ExecutableDiscovery().DiscoverAsync(
            new P4ExecutableDiscoveryOptions { ExecutablePath = executablePath });
        Assert.True(discovery.IsSuccess, discovery.Error?.Message);

        P4InfoResult result = await new P4InfoService(discovery).GetAsync();
        Assert.True(result.IsSuccess, result.Error?.Message);
    }

    private sealed class P4IntegrationFactAttribute : FactAttribute
    {
        public P4IntegrationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PERFORCE_MCP_TEST_P4_PATH")))
            {
                Skip = "Set PERFORCE_MCP_TEST_P4_PATH to run the Perforce integration test.";
            }
        }
    }

    private static P4ProcessResult Success(string output) =>
        new(0, output, string.Empty, false, false, TimeSpan.FromMilliseconds(5), null);

    private static P4ProcessResult Failure(string standardError) =>
        new(
            1,
            string.Empty,
            standardError,
            false,
            false,
            TimeSpan.FromMilliseconds(5),
            new P4ProcessError(P4ProcessErrorCode.NonZeroExit, "command failed"));

    private sealed class FakeRunner(P4ProcessResult result) : IP4ProcessRunner
    {
        public IReadOnlyList<string>? Arguments { get; private set; }

        public Task<P4ProcessResult> RunAsync(
            IReadOnlyList<string> arguments,
            P4ProcessRunnerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Arguments = arguments;
            return Task.FromResult(result);
        }
    }
}
