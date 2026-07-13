namespace PerforceMcp.Perforce.Tests;

public sealed class P4OpenedFilesServiceTests
{
    private const string FictionalOpenedFiles = """
        ... depotFile //Aurora/Main/Source/Hero.cpp
        ... clientFile D:\FictionalStudio\Aurora\Source\Hero.cpp
        ... action edit
        ... change default
        ... type text
        ... depotFile //Aurora/Main/Content/NewHero.uasset
        ... clientFile D:\FictionalStudio\Aurora\Content\NewHero.uasset
        ... action add
        ... change 4102
        ... type binary+l
        ... locked 1
        ... depotFile //Aurora/Main/Content/OldMap.umap
        ... clientFile D:\FictionalStudio\Aurora\Content\OldMap.umap
        ... action delete
        ... change 4102
        ... type binary
        ... depotFile //Aurora/Main/Content/OldName.uasset
        ... clientFile D:\FictionalStudio\Aurora\Content\OldName.uasset
        ... action move/delete
        ... change 4103
        ... type binary+l
        ... depotFile //Aurora/Main/Content/NewName.uasset
        ... clientFile D:\FictionalStudio\Aurora\Content\NewName.uasset
        ... action move/add
        ... change 4103
        ... type binary+l
        """;

    [Fact]
    public async Task RunsOnlyBoundedTaggedOpenedAndReturnsStructuredFields()
    {
        var runner = new FakeRunner(Success(FictionalOpenedFiles));
        var service = new P4OpenedFilesService(runner);

        P4OpenedFilesResult result = await service.GetAsync(10, "4102");

        Assert.Equal(["-ztag", "opened", "-m", "11", "-c", "4102"], runner.Arguments);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Count);
        Assert.Collection(
            result.Files,
            file => Assert.Equal("edit", file.Action),
            file =>
            {
                Assert.Equal("//Aurora/Main/Content/NewHero.uasset", file.DepotPath);
                Assert.Equal("D:\\FictionalStudio\\Aurora\\Content\\NewHero.uasset", file.LocalPath);
                Assert.Equal("4102", file.Changelist);
                Assert.Equal("binary+l", file.FileType);
                Assert.True(file.IsLocked);
                Assert.True(file.IsExclusiveOpen);
            },
            file => Assert.Equal("delete", file.Action),
            file => Assert.Equal("move/delete", file.Action),
            file => Assert.Equal("move/add", file.Action));
    }

    [Fact]
    public async Task ReturnsEmptySuccessWhenNothingIsOpened()
    {
        var service = new P4OpenedFilesService(new FakeRunner(Success(string.Empty)));

        P4OpenedFilesResult result = await service.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Files);
        Assert.Equal(0, result.Count);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public async Task IgnoresOpenedStreamSpecMetadataBeforeFiles()
    {
        const string fixture = """
            ... stream //Aurora/Main
            ... action edit
            ... change default
            ... depotFile //Aurora/Main/Source/Hero.cpp
            ... clientFile D:\FictionalStudio\Aurora\Source\Hero.cpp
            ... action edit
            ... change default
            ... type text
            """;
        var service = new P4OpenedFilesService(new FakeRunner(Success(fixture)));

        P4OpenedFilesResult result = await service.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Files);
        Assert.Equal("//Aurora/Main/Source/Hero.cpp", result.Files[0].DepotPath);
    }

    [Fact]
    public async Task TreatsNoOpenedFilesNonZeroExitAsEmptySuccess()
    {
        var service = new P4OpenedFilesService(new FakeRunner(Failure("File(s) not opened on this client.")));

        P4OpenedFilesResult result = await service.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Files);
    }

    [Fact]
    public async Task LimitsResultsAndIndicatesTruncation()
    {
        var service = new P4OpenedFilesService(new FakeRunner(Success(FictionalOpenedFiles)));

        P4OpenedFilesResult result = await service.GetAsync(2);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Files.Count);
        Assert.True(result.IsTruncated);
    }

    [Theory]
    [InlineData("Perforce password (P4PASSWD) invalid or unset.", P4OpenedFilesErrorCode.MissingLogin)]
    [InlineData("Connect to server failed; check $P4PORT.", P4OpenedFilesErrorCode.UnreachableServer)]
    [InlineData("Client 'missing-workspace' unknown.", P4OpenedFilesErrorCode.MissingClient)]
    [InlineData("Unexpected server failure.", P4OpenedFilesErrorCode.CommandFailed)]
    public async Task ClassifiesNonZeroExitsWithoutReturningRawOutput(string output, P4OpenedFilesErrorCode expected)
    {
        var service = new P4OpenedFilesService(new FakeRunner(Failure(output)));

        P4OpenedFilesResult result = await service.GetAsync();

        Assert.Equal(expected, result.Error?.Code);
        Assert.DoesNotContain("P4PASSWD", result.Error?.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("missing-workspace", result.Error?.Message ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("Unexpected server failure", result.Error?.Message ?? string.Empty, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not tagged output")]
    [InlineData("... action edit\n... change default\n... type text")]
    [InlineData("... depotFile //Aurora/Main/Hero.cpp\n... action edit\n... change default")]
    public async Task ReportsMalformedOutput(string output)
    {
        var service = new P4OpenedFilesService(new FakeRunner(Success(output)));

        P4OpenedFilesResult result = await service.GetAsync();

        Assert.Equal(P4OpenedFilesErrorCode.MalformedOutput, result.Error?.Code);
        Assert.Empty(result.Files);
    }

    [Fact]
    public async Task ReportsTimeoutWithoutReturningCapturedOutput()
    {
        var resultFromRunner = new P4ProcessResult(
            null,
            "ticket=FICTIONAL_SECRET",
            string.Empty,
            false,
            false,
            TimeSpan.FromSeconds(30),
            new P4ProcessError(P4ProcessErrorCode.TimedOut, "timed out"));
        var service = new P4OpenedFilesService(new FakeRunner(resultFromRunner));

        P4OpenedFilesResult result = await service.GetAsync();

        Assert.Equal(P4OpenedFilesErrorCode.TimedOut, result.Error?.Code);
        Assert.DoesNotContain("FICTIONAL_SECRET", result.Error?.Message ?? string.Empty, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(201, null)]
    [InlineData(50, "-a")]
    [InlineData(50, "0")]
    public async Task RejectsInvalidRequestsWithoutRunningP4(int limit, string? changelist)
    {
        var runner = new FakeRunner(Success(FictionalOpenedFiles));
        var service = new P4OpenedFilesService(runner);

        P4OpenedFilesResult result = await service.GetAsync(limit, changelist);

        Assert.Equal(P4OpenedFilesErrorCode.InvalidRequest, result.Error?.Code);
        Assert.Null(runner.Arguments);
    }

    [P4IntegrationFact]
    public async Task OptionalIntegrationTest()
    {
        string executablePath = Environment.GetEnvironmentVariable("PERFORCE_MCP_TEST_P4_PATH")!;
        P4ExecutableDiscoveryResult discovery = await new P4ExecutableDiscovery().DiscoverAsync(
            new P4ExecutableDiscoveryOptions { ExecutablePath = executablePath });
        Assert.True(discovery.IsSuccess, discovery.Error?.Message);

        P4OpenedFilesResult result = await new P4OpenedFilesService(discovery).GetAsync(10);

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

    private static P4ProcessResult Success(string output) =>
        new(0, output, string.Empty, false, false, TimeSpan.FromMilliseconds(5), null);

    private static P4ProcessResult Failure(string standardError) =>
        new(1, string.Empty, standardError, false, false, TimeSpan.FromMilliseconds(5), new P4ProcessError(P4ProcessErrorCode.NonZeroExit, "command failed"));

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
