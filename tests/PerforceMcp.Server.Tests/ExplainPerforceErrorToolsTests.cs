using ModelContextProtocol.Server;
using PerforceMcp.Perforce;
using PerforceMcp.Server;

namespace PerforceMcp.Server.Tests;

public sealed class ExplainPerforceErrorToolsTests
{
    [Fact]
    public void ExposesReadOnlyIdempotentToolAndDelegatesLocally()
    {
        P4ErrorExplanationResult result = ExplainPerforceErrorTools.ExplainPerforceError(errorCode: "MissingLogin");
        McpServerToolAttribute attribute = Assert.Single(
            typeof(ExplainPerforceErrorTools)
                .GetMethod(nameof(ExplainPerforceErrorTools.ExplainPerforceError))!
                .GetCustomAttributes(typeof(McpServerToolAttribute), inherit: false)
                .Cast<McpServerToolAttribute>());

        Assert.Equal("AUTH_REQUIRED", result.NormalizedCode);
        Assert.Equal("explain_perforce_error", attribute.Name);
        Assert.True(attribute.ReadOnly);
        Assert.True(attribute.Idempotent);
    }
}
