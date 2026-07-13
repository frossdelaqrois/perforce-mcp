namespace PerforceMcp.Perforce.Tests;

public sealed class P4PendingChangelistsServiceTests
{
    private const string FictionalInfo = """
        ... userName avery.dev
        ... clientName aurora-main-ws
        ... clientRoot D:\FictionalStudio\Aurora
        ... serverAddress perforce.example.test:1666
        ... serverVersion P4D/FICTIONAL/2025.1/1234567
        """;

    private const string FictionalChanges = """
        ... change 4103
        ... time 1750000100
        ... user avery.dev
        ... client aurora-main-ws
        ... status pending
        ... desc Rename hero assets
        ... change 4102
        ... time 1750000000
        ... user avery.dev
        ... client aurora-main-ws
        ... status pending
        ... desc Add the new hero
        """;

    private const string DefaultOpened = """
        ... depotFile //Aurora/Main/Source/Hero.cpp
        ... clientFile D:\FictionalStudio\Aurora\Source\Hero.cpp
        ... action edit
        ... change default
        ... type text
        """;

    private const string NumberedOpened = """
        ... depotFile //Aurora/Main/Content/Hero.uasset
        ... clientFile D:\FictionalStudio\Aurora\Content\Hero.uasset
        ... action edit
        ... change 4103
        ... type binary+l
        """;

    [Fact]
    public async Task ReturnsDefaultAndNumberedChangelistsWithOptionalFiles()
    {
        var runner = new SequenceRunner(
            Success(FictionalInfo),
            Success(FictionalChanges),
            Success(DefaultOpened),
            Success(NumberedOpened),
            NoOpened());
        var service = new P4PendingChangelistsService(runner);

        P4PendingChangelistsResult result = await service.GetAsync(10, includeFiles: true, fileLimit: 25);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Count);
        Assert.Equal(["-ztag", "info"], runner.Calls[0]);
        Assert.Equal(["-ztag", "changes", "-s", "pending", "-u", "avery.dev", "-c", "aurora-main-ws", "-t", "-L", "-m", "11"], runner.Calls[1]);
        Assert.Equal(["-ztag", "opened", "-m", "26", "-c", "default"], runner.Calls[2]);
        Assert.Collection(
            result.Changelists,
            change =>
            {
                Assert.Equal("default", change.Id);
                Assert.Null(change.Number);
                Assert.True(change.IsDefault);
                Assert.Null(change.ModifiedTime);
                Assert.Equal(1, change.FileCount);
                Assert.Single(change.Files);
            },
            change =>
            {
                Assert.Equal("4103", change.Id);
                Assert.Equal(4103, change.Number);
                Assert.False(change.IsDefault);
                Assert.Equal("Rename hero assets", change.Description);
                Assert.Equal("avery.dev", change.Owner);
                Assert.Equal("aurora-main-ws", change.Client);
                Assert.Equal("pending", change.Status);
                Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1750000100), change.ModifiedTime);
                Assert.Single(change.Files);
            },
            change =>
            {
                Assert.Equal(4102, change.Number);
                Assert.Equal(0, change.FileCount);
                Assert.Empty(change.Files);
            });
    }

    [Fact]
    public async Task ReturnsEmptyWhenNoDefaultOrNumberedChangelistsExist()
    {
        var runner = new SequenceRunner(Success(FictionalInfo), Success(string.Empty), NoOpened());
        var service = new P4PendingChangelistsService(runner);

        P4PendingChangelistsResult result = await service.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Changelists);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public async Task OmitsFilesByDefaultButStillReportsFileCount()
    {
        var runner = new SequenceRunner(Success(FictionalInfo), Success(string.Empty), Success(DefaultOpened));
        var service = new P4PendingChangelistsService(runner);

        P4PendingChangelistsResult result = await service.GetAsync();

        P4PendingChangelist change = Assert.Single(result.Changelists);
        Assert.Equal(1, change.FileCount);
        Assert.True(change.IsFileCountExact);
        Assert.Empty(change.Files);
        Assert.False(result.IncludesFiles);
    }

    [Fact]
    public async Task IndicatesChangelistAndFileTruncation()
    {
        string repeatedChanges = FictionalChanges + "\n" + """
            ... change 4101
            ... time 1749999900
            ... user avery.dev
            ... client aurora-main-ws
            ... status pending
            ... desc Third change
            """;
        string twoFiles = DefaultOpened + "\n" + NumberedOpened.Replace("4103", "default", StringComparison.Ordinal);
        var runner = new SequenceRunner(
            Success(FictionalInfo),
            Success(repeatedChanges),
            Success(twoFiles),
            NoOpened(),
            NoOpened(),
            NoOpened());
        var service = new P4PendingChangelistsService(runner);

        P4PendingChangelistsResult result = await service.GetAsync(limit: 2, includeFiles: true, fileLimit: 1);

        Assert.True(result.IsTruncated);
        Assert.Equal(2, result.Count);
        Assert.True(result.Changelists[0].FilesTruncated);
        Assert.False(result.Changelists[0].IsFileCountExact);
        Assert.Equal(1, result.Changelists[0].FileCount);
    }

    [Theory]
    [InlineData("not tagged output")]
    [InlineData("... user avery.dev\n... client aurora-main-ws")]
    [InlineData("... change nope\n... time 1750000100\n... user avery.dev\n... client aurora-main-ws\n... status pending\n... desc invalid")]
    public async Task ReportsMalformedChangeOutput(string output)
    {
        var runner = new SequenceRunner(Success(FictionalInfo), Success(output));
        var service = new P4PendingChangelistsService(runner);

        P4PendingChangelistsResult result = await service.GetAsync();

        Assert.Equal(P4PendingChangelistsErrorCode.MalformedOutput, result.Error?.Code);
        Assert.Empty(result.Changelists);
    }

    [Theory]
    [InlineData("Perforce password (P4PASSWD) invalid or unset.", P4PendingChangelistsErrorCode.MissingLogin)]
    [InlineData("Connect to server failed; check $P4PORT.", P4PendingChangelistsErrorCode.UnreachableServer)]
    [InlineData("Client 'missing-workspace' unknown.", P4PendingChangelistsErrorCode.MissingClient)]
    [InlineData("Unexpected failure with ticket=FICTIONAL_SECRET", P4PendingChangelistsErrorCode.CommandFailed)]
    public async Task ClassifiesChangeCommandFailuresWithoutRawOutput(string output, P4PendingChangelistsErrorCode expected)
    {
        var runner = new SequenceRunner(Success(FictionalInfo), Failure(output));
        var service = new P4PendingChangelistsService(runner);

        P4PendingChangelistsResult result = await service.GetAsync();

        Assert.Equal(expected, result.Error?.Code);
        Assert.DoesNotContain("P4PASSWD", result.Error?.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FICTIONAL_SECRET", result.Error?.Message ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("missing-workspace", result.Error?.Message ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsTimeout()
    {
        var runner = new SequenceRunner(
            Success(FictionalInfo),
            new P4ProcessResult(null, string.Empty, string.Empty, false, false, TimeSpan.FromSeconds(30), new P4ProcessError(P4ProcessErrorCode.TimedOut, "timed out")));
        var service = new P4PendingChangelistsService(runner);

        P4PendingChangelistsResult result = await service.GetAsync();

        Assert.Equal(P4PendingChangelistsErrorCode.TimedOut, result.Error?.Code);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(101, 100)]
    [InlineData(20, 0)]
    [InlineData(20, 201)]
    public async Task RejectsInvalidLimitsWithoutRunningP4(int limit, int fileLimit)
    {
        var runner = new SequenceRunner(Success(FictionalInfo));
        var service = new P4PendingChangelistsService(runner);

        P4PendingChangelistsResult result = await service.GetAsync(limit, fileLimit: fileLimit);

        Assert.Equal(P4PendingChangelistsErrorCode.InvalidRequest, result.Error?.Code);
        Assert.Empty(runner.Calls);
    }

    [P4IntegrationFact]
    public async Task OptionalIntegrationTest()
    {
        string executablePath = Environment.GetEnvironmentVariable("PERFORCE_MCP_TEST_P4_PATH")!;
        P4ExecutableDiscoveryResult discovery = await new P4ExecutableDiscovery().DiscoverAsync(
            new P4ExecutableDiscoveryOptions { ExecutablePath = executablePath });
        Assert.True(discovery.IsSuccess, discovery.Error?.Message);

        P4PendingChangelistsResult result = await new P4PendingChangelistsService(discovery).GetAsync(10, false, 20);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.InRange(result.Count, 0, 10);
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

    private static P4ProcessResult Success(string output) => new(0, output, string.Empty, false, false, TimeSpan.FromMilliseconds(5), null);
    private static P4ProcessResult NoOpened() => Failure("File(s) not opened on this client.");
    private static P4ProcessResult Failure(string error) => new(1, string.Empty, error, false, false, TimeSpan.FromMilliseconds(5), new P4ProcessError(P4ProcessErrorCode.NonZeroExit, "failed"));

    private sealed class SequenceRunner(params P4ProcessResult[] results) : IP4ProcessRunner
    {
        private readonly Queue<P4ProcessResult> _results = new(results);
        public List<IReadOnlyList<string>> Calls { get; } = [];

        public Task<P4ProcessResult> RunAsync(IReadOnlyList<string> arguments, P4ProcessRunnerOptions? options = null, CancellationToken cancellationToken = default)
        {
            Calls.Add(arguments.ToArray());
            return Task.FromResult(_results.Dequeue());
        }
    }
}
