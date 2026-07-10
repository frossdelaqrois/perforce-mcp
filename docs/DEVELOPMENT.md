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

The scaffold does not start an MCP server or invoke Perforce yet. Those behaviours are added by later read-only milestone issues.

## Perforce executable discovery

The Perforce adapter accepts an absolute configured path to `p4` or `p4.exe` and otherwise searches absolute directories in `PATH`. It validates candidates by invoking the executable directly with the `-V` argument, without a command shell, and returns the validated path and `Rev. P4/...` version line. Validation has a five-second default timeout and captures at most 32 KiB from each output stream.

Discovery does not connect to a Perforce server, run `p4 info` or other operational commands, parse repository data, add network functionality, or expose an MCP tool.
