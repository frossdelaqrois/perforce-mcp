using System.ComponentModel;
using ModelContextProtocol.Server;
using PerforceMcp.Perforce;

namespace PerforceMcp.Server;

[McpServerToolType]
public sealed class PerforceInfoTools(P4InfoService infoService)
{
    [McpServerTool(Name = "get_perforce_info", ReadOnly = true, Idempotent = true)]
    [Description("Get the active Perforce server, user, workspace, client root, and server version. Does not return credentials or environment variables.")]
    public Task<P4InfoResult> GetPerforceInfo(CancellationToken cancellationToken) =>
        infoService.GetAsync(cancellationToken);
}
