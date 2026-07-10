namespace PerforceMcp.Perforce.Tests;

public sealed class P4ExecutableDiscoveryTests
{
    [Fact]
    public async Task UsesConfiguredExecutableBeforePathDiscovery()
    {
        string configuredPath = CreateAbsolutePath("configured", "p4.exe");
        string pathExecutable = CreateAbsolutePath("from-path", "p4.exe");
        var platform = new FakePlatform
        {
            PathValue = Path.GetDirectoryName(pathExecutable),
        };
        platform.ExistingFiles.Add(configuredPath);
        platform.ExistingFiles.Add(pathExecutable);
        var validator = new FakeValidator(P4ExecutableValidationStatus.Valid);
        var discovery = new P4ExecutableDiscovery(platform, validator);

        P4ExecutableDiscoveryResult result = await discovery.DiscoverAsync(
            new P4ExecutableDiscoveryOptions { ExecutablePath = configuredPath });

        Assert.True(result.IsSuccess);
        Assert.Equal(configuredPath, result.ExecutablePath);
        Assert.Equal(FakeValidator.ValidVersion, result.Version);
        Assert.Equal([configuredPath], validator.ValidatedPaths);
    }

    [Fact]
    public async Task DiscoversExecutableFromPath()
    {
        string missingDirectory = CreateAbsolutePath("missing");
        string executablePath = CreateAbsolutePath("perforce", "p4.exe");
        string executableDirectory = Path.GetDirectoryName(executablePath)!;
        var platform = new FakePlatform
        {
            PathValue = string.Join(
                Path.PathSeparator,
                missingDirectory,
                executableDirectory),
        };
        platform.ExistingFiles.Add(executablePath);
        var validator = new FakeValidator(P4ExecutableValidationStatus.Valid);
        var discovery = new P4ExecutableDiscovery(platform, validator);

        P4ExecutableDiscoveryResult result = await discovery.DiscoverAsync(
            new P4ExecutableDiscoveryOptions());

        Assert.True(result.IsSuccess);
        Assert.Equal(executablePath, result.ExecutablePath);
        Assert.Equal([executablePath], validator.ValidatedPaths);
    }

    [Fact]
    public async Task ReturnsActionableErrorWhenExecutableIsMissing()
    {
        var platform = new FakePlatform { PathValue = null };
        var validator = new FakeValidator(P4ExecutableValidationStatus.Valid);
        var discovery = new P4ExecutableDiscovery(platform, validator);

        P4ExecutableDiscoveryResult result = await discovery.DiscoverAsync(
            new P4ExecutableDiscoveryOptions());

        Assert.False(result.IsSuccess);
        Assert.Equal(P4ExecutableDiscoveryErrorCode.NotFound, result.Error?.Code);
        Assert.Contains("Configure an absolute path", result.Error?.Message);
        Assert.Empty(validator.ValidatedPaths);
    }

    [Fact]
    public async Task RejectsExecutableThatDoesNotIdentifyAsPerforce()
    {
        string executablePath = CreateAbsolutePath("invalid", "p4.exe");
        var platform = new FakePlatform();
        platform.ExistingFiles.Add(executablePath);
        var validator = new FakeValidator(P4ExecutableValidationStatus.Invalid);
        var discovery = new P4ExecutableDiscovery(platform, validator);

        P4ExecutableDiscoveryResult result = await discovery.DiscoverAsync(
            new P4ExecutableDiscoveryOptions { ExecutablePath = executablePath });

        Assert.False(result.IsSuccess);
        Assert.Equal(P4ExecutableDiscoveryErrorCode.ValidationFailed, result.Error?.Code);
        Assert.Contains("did not identify itself", result.Error?.Message);
    }

    [Fact]
    public async Task AppliesConfiguredValidationTimeout()
    {
        string executablePath = CreateAbsolutePath("slow", "p4.exe");
        var platform = new FakePlatform();
        platform.ExistingFiles.Add(executablePath);
        var validator = new FakeValidator(P4ExecutableValidationStatus.TimedOut);
        var discovery = new P4ExecutableDiscovery(platform, validator);
        TimeSpan timeout = TimeSpan.FromMilliseconds(250);

        P4ExecutableDiscoveryResult result = await discovery.DiscoverAsync(
            new P4ExecutableDiscoveryOptions
            {
                ExecutablePath = executablePath,
                ValidationTimeout = timeout,
            });

        Assert.Equal(P4ExecutableDiscoveryErrorCode.ValidationTimedOut, result.Error?.Code);
        Assert.Equal([timeout], validator.ValidationTimeouts);
    }

    [Fact]
    public async Task RejectsArbitraryExecutableNameWithoutStartingIt()
    {
        string executablePath = CreateAbsolutePath("tools", "not-p4.exe");
        var platform = new FakePlatform();
        platform.ExistingFiles.Add(executablePath);
        var validator = new FakeValidator(P4ExecutableValidationStatus.Valid);
        var discovery = new P4ExecutableDiscovery(platform, validator);

        P4ExecutableDiscoveryResult result = await discovery.DiscoverAsync(
            new P4ExecutableDiscoveryOptions { ExecutablePath = executablePath });

        Assert.Equal(P4ExecutableDiscoveryErrorCode.InvalidExecutable, result.Error?.Code);
        Assert.Empty(validator.ValidatedPaths);
    }

    private static string CreateAbsolutePath(params string[] segments)
    {
        string path = Path.Combine([Path.GetTempPath(), "perforce-mcp-tests", .. segments]);
        return Path.GetFullPath(path);
    }

    private sealed class FakePlatform : IP4ExecutablePlatform
    {
        public HashSet<string> ExistingFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string ExecutableFileName => "p4.exe";

        public char PathSeparator => Path.PathSeparator;

        public string? PathValue { get; init; }

        public string Combine(string path1, string path2) => Path.Combine(path1, path2);

        public bool FileExists(string path) => ExistingFiles.Contains(path);

        public string GetFullPath(string path) => Path.GetFullPath(path);

        public bool IsPathFullyQualified(string path) => Path.IsPathFullyQualified(path);
    }

    private sealed class FakeValidator(P4ExecutableValidationStatus status) : IP4ExecutableValidator
    {
        public const string ValidVersion = "Rev. P4/NTX64/2025.1/1234567 (2025/01/01).";

        public List<string> ValidatedPaths { get; } = [];

        public List<TimeSpan> ValidationTimeouts { get; } = [];

        public Task<P4ExecutableValidationResult> ValidateAsync(
            string executablePath,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidatedPaths.Add(executablePath);
            ValidationTimeouts.Add(timeout);
            string? version = status is P4ExecutableValidationStatus.Valid ? ValidVersion : null;
            return Task.FromResult(new P4ExecutableValidationResult(status, version));
        }
    }
}
