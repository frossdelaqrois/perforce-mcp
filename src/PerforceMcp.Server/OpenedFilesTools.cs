using System.ComponentModel;
using ModelContextProtocol.Server;
using PerforceMcp.Perforce;

namespace PerforceMcp.Server;

[McpServerToolType]
public sealed class OpenedFilesTools(P4OpenedFilesService openedFilesService)
{
    [McpServerTool(Name = "get_opened_files", ReadOnly = true, Idempotent = true)]
    [Description("List files opened in the active Perforce workspace as structured data. Supports an optional pending changelist filter and a result limit; never returns raw p4 output or file contents.")]
    public Task<P4OpenedFilesResult> GetOpenedFiles(
        [Description("Maximum files to return, from 1 to 200. Defaults to 50.")] int limit = P4OpenedFilesService.DefaultLimit,
        [Description("Optional pending changelist number, or 'default'.")] string? changelist = null,
        CancellationToken cancellationToken = default) =>
        openedFilesService.GetAsync(limit, changelist, cancellationToken);
}
