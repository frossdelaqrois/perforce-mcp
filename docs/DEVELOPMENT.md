# Development Setup

The repository contains the initial .NET 10 solution scaffold for the read-only prototype.

## Prerequisites

- Windows 11 for initial end-to-end testing
- A currently supported .NET LTS SDK
- Perforce command-line client (`p4`)
- Access to a test Perforce server and non-production workspace
- An MCP-compatible test client

## Initial workflow

1. Clone the repository.
2. Restore .NET dependencies with `dotnet restore PerforceMcp.slnx`.
3. Build the solution with `dotnet build PerforceMcp.slnx --no-restore`.
4. Run unit tests with `dotnet test PerforceMcp.slnx --no-build`.
5. Configure test Perforce settings outside source control.
6. Start the MCP server locally.
7. Call read-only tools from the MCP client.

## Configuration rules

- Never commit passwords, tickets, tokens, or private server details.
- Prefer normal Perforce configuration mechanisms and environment variables.
- Test fixtures must use fictional users, servers, clients, and depot paths.

## Perforce executable discovery

The Perforce adapter accepts an absolute configured path to `p4` or `p4.exe` and otherwise searches absolute directories in `PATH`. It validates candidates by invoking the executable directly with the `-V` argument, without a command shell, and returns the validated path and `Rev. P4/...` version line. Validation has a five-second default timeout and captures at most 32 KiB from each output stream.

Discovery does not connect to a Perforce server, run `p4 info` or other operational commands, parse repository data, add network functionality, or expose an MCP tool.

## Manual `get_opened_files` testing

Start the MCP server with a non-production Perforce workspace, then try:

- “Show my opened Perforce files.”
- “Group my opened files by changelist.”
- “How many Unreal assets do I currently have checked out?”
- “Which opened files are `.uasset` or `.umap` files?”
- Request a small limit and confirm `isTruncated` when more files are open.
- Filter by `default` and by a numbered pending changelist.

Also verify a workspace with no opened files returns a successful empty list. For reversible failure testing, temporarily select a nonexistent client or unreachable test endpoint, confirm the structured `MissingClient` or `UnreachableServer` error, and restore the original configuration in a cleanup step. Do not run these tests against production settings or log credentials, tickets, environment dumps, or raw command output.

Set `PERFORCE_MCP_TEST_P4_PATH` to an absolute configured test `p4` executable to enable the optional integration tests. They remain skipped by default.

## Manual `get_pending_changelists` testing

With the MCP server connected to a non-production workspace, try:

- “Show my pending Perforce changelists.”
- “Which pending changelist was modified most recently?”
- “Include files in my pending changelists.”
- “Summarize the default changelist separately from numbered changelists.”
- Request small changelist and file limits and confirm both truncation states are explicit.

Verify an empty workspace returns a successful empty list, an opened default changelist has `isDefault: true` and no number, and numbered changelists include owner, client, status, modified time, and bounded file-count metadata. Reuse the reversible connection/workspace failure checks above and restore the original configuration in a cleanup step.
