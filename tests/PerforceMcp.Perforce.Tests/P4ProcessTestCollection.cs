namespace PerforceMcp.Perforce.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class P4ProcessTestGroup
{
    public const string Name = "P4 process tests";
}
