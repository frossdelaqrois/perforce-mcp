using System.ComponentModel;
using ModelContextProtocol.Server;
using PerforceMcp.Perforce;

namespace PerforceMcp.Server;

[McpServerToolType]
public sealed class ExplainPerforceErrorTools
{
    [McpServerTool(Name = "explain_perforce_error", ReadOnly = true, Idempotent = true)]
    [Description("Explain a bounded Perforce error using deterministic local classification. Returns observed facts separately from possible causes and safe verification steps. Redacts sensitive indicators, never echoes raw error text, and does not execute commands or read files or environment variables.")]
    public static P4ErrorExplanationResult ExplainPerforceError(
        [Description("Optional untrusted Perforce error text, limited to 4096 characters. The original text is never returned.")] string? errorText = null,
        [Description("Optional known Perforce Companion structured error code, limited to 64 characters. At least one input is required.")] string? errorCode = null) =>
        P4ErrorExplanationService.Explain(errorText, errorCode);
}
