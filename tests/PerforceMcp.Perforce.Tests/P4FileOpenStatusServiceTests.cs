namespace PerforceMcp.Perforce.Tests;

public sealed class P4FileOpenStatusServiceTests
{
    private const string Info = """
        ... userName Ada
        ... clientName Aurora_Ada
        ... clientRoot D:\FictionalStudio\Aurora
        """;
    private const string MapMetadata = """
        ... depotFile //Aurora/Main/Content/MainLevel.umap
        ... clientFile D:\FictionalStudio\Aurora\Content\MainLevel.umap
        ... headType binary+l
        """;

    [Fact]
    public async Task ResolvesExactDepotPathWithFixedReadOnlyCommands()
    {
        var runner = new QueueRunner(Success(Info), Success(MapMetadata), Success(string.Empty));
        P4FileOpenStatusResult result = await new P4FileOpenStatusService(runner).GetAsync("//Aurora/Main/Content/MainLevel.umap");

        Assert.True(result.IsSuccess);
        P4FileOpenMatch match = Assert.Single(result.Matches);
        Assert.True(match.IsUnrealAsset);
        Assert.False(match.IsOpen);
        Assert.False(match.IsBlocking);
        Assert.Equal(["-ztag", "info"], runner.Calls[0]);
        Assert.Equal(["-ztag", "fstat", "-m", "11", "-T", "depotFile,clientFile,headType", "//Aurora/Main/Content/MainLevel.umap"], runner.Calls[1]);
        Assert.Equal("opened", runner.Calls[2][1]);
        Assert.Contains("-a", runner.Calls[2]);
    }

    [Fact]
    public async Task ResolvesLocalPathInsideWorkspace()
    {
        var runner = new QueueRunner(Success(Info), Success(MapMetadata), Success(string.Empty));
        P4FileOpenStatusResult result = await new P4FileOpenStatusService(runner)
            .GetAsync("""D:\FictionalStudio\Aurora\Content\MainLevel.umap""");

        Assert.True(result.IsSuccess);
        Assert.Equal("""D:\FictionalStudio\Aurora\Content\MainLevel.umap""", runner.Calls[1][^1]);
    }

    [Fact]
    public async Task RejectsOutsideWorkspaceAndWildcardsBeforeFileLookup()
    {
        var outsideRunner = new QueueRunner(Success(Info));
        P4FileOpenStatusResult outside = await new P4FileOpenStatusService(outsideRunner).GetAsync("""D:\Other\Secret.uasset""");
        P4FileOpenStatusResult wildcard = await new P4FileOpenStatusService(new QueueRunner()).GetAsync("*.umap");

        Assert.Equal(P4FileOpenStatusErrorCode.InvalidRequest, outside.Error?.Code);
        Assert.Single(outsideRunner.Calls);
        Assert.Equal(P4FileOpenStatusErrorCode.InvalidRequest, wildcard.Error?.Code);
    }

    [Fact]
    public async Task ReturnsBoundedAmbiguousExactFilenameMatchesWithoutGuessing()
    {
        const string files = """
            ... depotFile //Aurora/Main/Maps/MainLevel.umap
            ... rev 4
            ... depotFile //Aurora/DLC/Maps/MainLevel.umap
            ... rev 2
            ... depotFile //Aurora/Archive/MainLevel.umap
            ... rev 1
            """;
        const string metadata = """
            ... depotFile //Aurora/Main/Maps/MainLevel.umap
            ... clientFile D:\FictionalStudio\Aurora\Maps\MainLevel.umap
            ... headType binary+l
            ... depotFile //Aurora/DLC/Maps/MainLevel.umap
            ... clientFile D:\FictionalStudio\Aurora\DLC\MainLevel.umap
            ... headType binary+l
            """;
        var runner = new QueueRunner(Success(Info), Success(files), Success(metadata), Success(string.Empty));
        P4FileOpenStatusResult result = await new P4FileOpenStatusService(runner).GetAsync("MainLevel.umap", 2);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsAmbiguous);
        Assert.True(result.IsTruncated);
        Assert.Equal(2, result.Count);
        Assert.Equal(["-ztag", "files", "-m", "3", "//.../MainLevel.umap"], runner.Calls[1]);
    }

    [Fact]
    public async Task DistinguishesCurrentUserOtherUsersAndBlockingLocks()
    {
        const string opened = """
            ... depotFile //Aurora/Main/Content/MainLevel.umap
            ... user Ada
            ... client Aurora_Ada
            ... action edit
            ... change default
            ... type binary+l
            ... depotFile //Aurora/Main/Content/MainLevel.umap
            ... user Grace
            ... client Aurora_Grace
            ... action edit
            ... change 4107
            ... type binary+l
            ... locked 1
            """;
        var runner = new QueueRunner(Success(Info), Success(MapMetadata), Success(opened));
        P4FileOpenMatch match = Assert.Single((await new P4FileOpenStatusService(runner)
            .GetAsync("//Aurora/Main/Content/MainLevel.umap")).Matches);

        Assert.True(match.IsOpen);
        Assert.True(match.IsBlocking);
        Assert.Collection(match.Opens,
            own => { Assert.True(own.IsCurrentUser); Assert.True(own.IsCurrentClient); Assert.True(own.IsCurrentWorkspaceOpen); Assert.False(own.BlocksCurrentUser); },
            other => { Assert.False(other.IsCurrentUser); Assert.False(other.IsCurrentClient); Assert.False(other.IsCurrentWorkspaceOpen); Assert.True(other.IsLocked); Assert.True(other.IsExclusiveOpen); Assert.True(other.BlocksCurrentUser); });
    }

    [Fact]
    public async Task OtherUsersNonExclusiveOpenDoesNotBlock()
    {
        const string textMetadata = """
            ... depotFile //Aurora/Main/Source/Hero.cpp
            ... clientFile D:\FictionalStudio\Aurora\Source\Hero.cpp
            ... headType text
            """;
        const string opened = """
            ... depotFile //Aurora/Main/Source/Hero.cpp
            ... user Grace
            ... client Aurora_Grace
            ... action edit
            ... change 4108
            ... type text
            """;
        var runner = new QueueRunner(Success(Info), Success(textMetadata), Success(opened));
        P4FileOpenMatch match = Assert.Single((await new P4FileOpenStatusService(runner).GetAsync("//Aurora/Main/Source/Hero.cpp")).Matches);

        Assert.True(match.IsOpen);
        Assert.False(match.IsBlocking);
        Assert.False(match.Opens[0].BlocksCurrentUser);
    }

    [Fact]
    public async Task VisibleLockOnNonExclusiveFileDoesNotProveEditingIsBlocked()
    {
        const string metadata = """
            ... depotFile //Aurora/Main/Source/Hero.cpp
            ... clientFile D:\FictionalStudio\Aurora\Source\Hero.cpp
            ... headType text
            """;
        const string opened = """
            ... depotFile //Aurora/Main/Source/Hero.cpp
            ... user Grace
            ... client Aurora_Grace
            ... action edit
            ... change 4109
            ... type text
            ... locked 1
            """;
        var runner = new QueueRunner(Success(Info), Success(metadata), Success(opened));
        P4FileOpenMatch match = Assert.Single((await new P4FileOpenStatusService(runner).GetAsync("//Aurora/Main/Source/Hero.cpp")).Matches);

        Assert.True(match.Opens[0].IsLocked);
        Assert.False(match.Opens[0].IsExclusiveOpen);
        Assert.False(match.Opens[0].BlocksCurrentUser);
        Assert.False(match.IsBlocking);
        Assert.Contains("none proves", match.BlockingReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SameUserExclusiveOpenInAnotherWorkspaceBlocksCurrentWorkspace()
    {
        const string opened = """
            ... depotFile //Aurora/Main/Content/MainLevel.umap
            ... user Ada
            ... client Aurora_Ada_Laptop
            ... action edit
            ... change 4110
            ... type binary+l
            """;
        var runner = new QueueRunner(Success(Info), Success(MapMetadata), Success(opened));
        P4FileOpenMatch match = Assert.Single((await new P4FileOpenStatusService(runner).GetAsync("//Aurora/Main/Content/MainLevel.umap")).Matches);
        P4FileOpenRecord record = Assert.Single(match.Opens);

        Assert.True(record.IsCurrentUser);
        Assert.False(record.IsCurrentClient);
        Assert.False(record.IsCurrentWorkspaceOpen);
        Assert.True(record.IsExclusiveOpen);
        Assert.True(record.BlocksCurrentUser);
        Assert.True(match.IsBlocking);
    }

    [Fact]
    public async Task ClassifiesUassetAsUnrealAsset()
    {
        const string metadata = """
            ... depotFile //Aurora/Main/Content/Hero.uasset
            ... clientFile D:\FictionalStudio\Aurora\Content\Hero.uasset
            ... headType binary+l
            """;
        var runner = new QueueRunner(Success(Info), Success(metadata), Success(string.Empty));
        P4FileOpenMatch match = Assert.Single((await new P4FileOpenStatusService(runner)
            .GetAsync("//Aurora/Main/Content/Hero.uasset")).Matches);
        Assert.True(match.IsUnrealAsset);
    }

    [Fact]
    public async Task ReturnsStructuredMissingFile()
    {
        var runner = new QueueRunner(Success(Info), Failure("no such file(s)."));
        P4FileOpenStatusResult result = await new P4FileOpenStatusService(runner).GetAsync("//Aurora/Main/Missing.uasset");
        Assert.Equal(P4FileOpenStatusErrorCode.MissingFile, result.Error?.Code);
        Assert.DoesNotContain("no such", result.Error?.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandlesTimeoutAndMalformedOutput()
    {
        var timeout = new P4ProcessResult(null, string.Empty, "secret", false, false, TimeSpan.FromSeconds(30), new(P4ProcessErrorCode.TimedOut, "timeout"));
        P4FileOpenStatusResult timedOut = await new P4FileOpenStatusService(new QueueRunner(timeout)).GetAsync("MainLevel.umap");
        P4FileOpenStatusResult malformed = await new P4FileOpenStatusService(new QueueRunner(Success(Info), Success("not tagged"))).GetAsync("//Aurora/Main/Bad.uasset");
        Assert.Equal(P4FileOpenStatusErrorCode.TimedOut, timedOut.Error?.Code);
        Assert.Equal(P4FileOpenStatusErrorCode.MalformedOutput, malformed.Error?.Code);
    }

    [P4IntegrationFact]
    public async Task OptionalIntegrationTest()
    {
        string executablePath = Environment.GetEnvironmentVariable("PERFORCE_MCP_TEST_P4_PATH")!;
        string query = Environment.GetEnvironmentVariable("PERFORCE_MCP_TEST_FILE") ?? "definitely-missing-fictional-file.uasset";
        P4ExecutableDiscoveryResult discovery = await new P4ExecutableDiscovery().DiscoverAsync(new() { ExecutablePath = executablePath });
        Assert.True(discovery.IsSuccess, discovery.Error?.Message);
        P4FileOpenStatusResult result = await new P4FileOpenStatusService(discovery).GetAsync(query);
        Assert.True(result.IsSuccess || result.Error?.Code == P4FileOpenStatusErrorCode.MissingFile, result.Error?.Message);
    }

    private sealed class P4IntegrationFactAttribute : FactAttribute
    {
        public P4IntegrationFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PERFORCE_MCP_TEST_P4_PATH"))) Skip = "Set PERFORCE_MCP_TEST_P4_PATH to run the Perforce integration test."; }
    }
    private static P4ProcessResult Success(string output) => new(0, output, string.Empty, false, false, TimeSpan.FromMilliseconds(5), null);
    private static P4ProcessResult Failure(string error) => new(1, string.Empty, error, false, false, TimeSpan.FromMilliseconds(5), new(P4ProcessErrorCode.NonZeroExit, "failed"));
    private sealed class QueueRunner(params P4ProcessResult[] results) : IP4ProcessRunner
    {
        private readonly Queue<P4ProcessResult> _results = new(results);
        public List<IReadOnlyList<string>> Calls { get; } = [];
        public Task<P4ProcessResult> RunAsync(IReadOnlyList<string> arguments, P4ProcessRunnerOptions? options = null, CancellationToken cancellationToken = default)
        { Calls.Add(arguments); return Task.FromResult(_results.Dequeue()); }
    }
}
