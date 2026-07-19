using System.ComponentModel;
using ModelContextProtocol.Server;
using PerforceMcp.Perforce;

namespace PerforceMcp.Server;

[McpServerToolType]
public sealed class FileOpenStatusTools(P4FileOpenStatusService service)
{
    [McpServerTool(Name = "get_file_open_status", ReadOnly = true, Idempotent = true)]
    [Description("Resolve one depot path, in-workspace local path, or exact filename and report who has each bounded match open. Editing is reported as blocked only for an exclusive-open (+l) record outside the current user and client; observed lock state is reported separately. Never unlocks or modifies files.")]
    public Task<P4FileOpenStatusResult> GetFileOpenStatus(
        [Description("Depot path, local path within the active workspace, or exact filename without wildcards.")] string query,
        [Description("Maximum exact-filename matches to return, from 1 to 25. Defaults to 10.")] int limit = P4FileOpenStatusService.DefaultLimit,
        CancellationToken cancellationToken = default) => service.GetAsync(query, limit, cancellationToken);
}
