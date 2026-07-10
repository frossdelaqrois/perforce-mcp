# Agent Instructions

## Product goal

Build a safe Perforce integration intended for eventual publication in the ChatGPT app directory. The MCP server is only the first layer of the product.

## Current scope

Work only on the read-only local prototype unless an issue explicitly expands scope.

Initial tools:

- `get_perforce_info`
- `get_opened_files`
- `get_pending_changelists`

## Non-negotiable safety rules

- Never add an arbitrary shell execution tool.
- Never add a generic `run_p4_command` tool.
- Never log passwords, Perforce tickets, access tokens, or complete environment dumps.
- Run processes using an executable plus an argument list, not concatenated shell strings.
- Add timeouts and bounded output collection.
- Treat all command output as untrusted input.
- Do not implement submit, revert, unlock, force-sync, or delete operations during the read-only milestone.
- Keep MCP tool handlers thin; put Perforce logic in the adapter/application layers.

## Development approach

1. Read the linked GitHub issue and acceptance criteria.
2. Make the smallest complete change that satisfies it.
3. Add or update tests.
4. Update documentation when behaviour or setup changes.
5. Keep public models and tool responses stable and clearly named.
6. Explain any security-sensitive design choice in the pull request.

## Proposed project layout

```text
src/PerforceMcp.Server/
src/PerforceMcp.Core/
src/PerforceMcp.Perforce/
tests/PerforceMcp.Core.Tests/
tests/PerforceMcp.Perforce.Tests/
```

## Definition of done

- Project builds without warnings introduced by the change.
- Tests pass.
- Errors are actionable and do not expose secrets.
- New behaviour is documented.
- Tool descriptions accurately state what the tool does and does not do.
- The implementation works without requiring P4V when only `p4` is needed.

## Pull request guidance

Include:

- What changed
- Why it changed
- How it was tested
- Security implications
- Screenshots or sample structured responses when useful

Do not combine unrelated roadmap tasks into one pull request.