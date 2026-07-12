namespace PerforceMcp.Perforce;

internal interface IP4ExecutablePlatform
{
    string ExecutableFileName { get; }

    char PathSeparator { get; }

    string? PathValue { get; }

    string Combine(string path1, string path2);

    bool FileExists(string path);

    string GetFullPath(string path);

    bool IsPathFullyQualified(string path);
}

internal sealed class SystemP4ExecutablePlatform : IP4ExecutablePlatform
{
    public string ExecutableFileName => OperatingSystem.IsWindows() ? "p4.exe" : "p4";

    public char PathSeparator => Path.PathSeparator;

    public string? PathValue => Environment.GetEnvironmentVariable("PATH");

    public string Combine(string path1, string path2) => Path.Combine(path1, path2);

    public bool FileExists(string path) => File.Exists(path);

    public string GetFullPath(string path) => Path.GetFullPath(path);

    public bool IsPathFullyQualified(string path) => Path.IsPathFullyQualified(path);
}
