namespace PerforceMcp.Core.Tests;

public class ProjectMarkerTests
{
    [Fact]
    public void CoreAssemblyHasExpectedName()
    {
        Assert.Equal("PerforceMcp.Core", typeof(Core.ProjectMarker).Assembly.GetName().Name);
    }
}
