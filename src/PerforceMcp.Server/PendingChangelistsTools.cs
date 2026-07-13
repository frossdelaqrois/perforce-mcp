using System.ComponentModel;
using ModelContextProtocol.Server;
using PerforceMcp.Perforce;

namespace PerforceMcp.Server;

[McpServerToolType]
public sealed class PendingChangelistsTools(P4PendingChangelistsService pendingChangelistsService)
{
    [McpServerTool(Name = "get_pending_changelists", ReadOnly = true, Idempotent = true)]
    [Description("List pending changelists for the current Perforce user and workspace as structured data, including the default changelist when it contains opened files. File details are optional and strictly bounded; raw p4 output and file contents are never returned.")]
    public Task<P4PendingChangelistsResult> GetPendingChangelists(
        [Description("Maximum changelists to return, from 1 to 100. Defaults to 20.")] int limit = P4PendingChangelistsService.DefaultLimit,
        [Description("Include structured opened-file metadata for each changelist. Defaults to false.")] bool includeFiles = false,
        [Description("Maximum files inspected and optionally returned per changelist, from 1 to 200. Defaults to 100.")] int fileLimit = P4PendingChangelistsService.DefaultFileLimit,
        CancellationToken cancellationToken = default) =>
        pendingChangelistsService.GetAsync(limit, includeFiles, fileLimit, cancellationToken);
}
