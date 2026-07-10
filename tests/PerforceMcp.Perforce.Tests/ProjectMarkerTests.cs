namespace PerforceMcp.Perforce.Tests;

public class ProjectMarkerTests
{
    [Fact]
    public void PerforceAssemblyHasExpectedName()
    {
        Assert.Equal("PerforceMcp.Perforce", typeof(Perforce.ProjectMarker).Assembly.GetName().Name);
    }
}
