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
