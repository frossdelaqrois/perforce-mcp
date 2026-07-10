# Development Setup

The implementation has not been scaffolded yet. This document records the intended first setup.

## Prerequisites

- Windows 11 for initial end-to-end testing
- A currently supported .NET LTS SDK
- Perforce command-line client (`p4`)
- Access to a test Perforce server and non-production workspace
- An MCP-compatible test client

## Initial workflow

1. Clone the repository.
2. Restore .NET dependencies.
3. Build the solution.
4. Run unit tests.
5. Configure test Perforce settings outside source control.
6. Start the MCP server locally.
7. Call read-only tools from the MCP client.

## Configuration rules

- Never commit passwords, tickets, tokens, or private server details.
- Prefer normal Perforce configuration mechanisms and environment variables.
- Test fixtures must use fictional users, servers, clients, and depot paths.

Exact commands will be added by the project-scaffolding issue.