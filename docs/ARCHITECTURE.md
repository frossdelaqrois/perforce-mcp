# Architecture

## Overview

The product needs both a hosted component and a local component because Perforce commands normally run against credentials and workspaces available on a developer's machine or private studio network.

```text
ChatGPT
  |
  | MCP over HTTPS
  v
Hosted Gateway
  |
  | encrypted, authenticated device channel
  v
Local Companion
  |
  +-- Perforce CLI (`p4`)
  +-- P4V
  +-- UnrealGameSync
  +-- local workspace files
```

## Components

### ChatGPT app

Responsibilities:

- Present onboarding and connection state
- Expose well-described MCP tools
- Render structured results
- Request confirmation for consequential actions
- Avoid displaying secrets or unnecessarily sensitive paths

### Hosted gateway

Responsibilities:

- Authenticate users
- Pair and identify authorised devices
- Route requests to the correct device
- Validate request schemas and permissions
- Enforce rate limits and tenant isolation
- Keep minimal audit records
- Never store Perforce passwords when avoidable

### Local companion

Responsibilities:

- Discover and validate `p4`
- Use explicit argument arrays rather than arbitrary shell strings
- Apply an allowlist of supported operations
- Run commands with timeouts and output limits
- Parse responses into stable structured types
- Display and record pending write requests
- Store device credentials in OS-provided secure storage

### Perforce adapter

Responsibilities:

- Build safe command arguments
- Prefer machine-readable tagged output
- Normalise errors
- Redact tickets, tokens, and sensitive environment values
- Remain independent of the MCP transport layer

## Initial repository structure

```text
src/
  PerforceMcp.Server/       MCP transport and tool definitions
  PerforceMcp.Core/         application services and domain models
  PerforceMcp.Perforce/     safe `p4` process adapter and parsers
tests/
  PerforceMcp.Core.Tests/
  PerforceMcp.Perforce.Tests/
docs/
  ARCHITECTURE.md
  SECURITY.md
```

## Initial technology choice

Recommended starting point: **C# on current supported .NET LTS**.

Reasons:

- Strong fit for Windows-based Unreal and P4V environments
- Good process, service, installer, and credential-storage support
- Cross-platform capability for the MCP server core
- Strong typing for tool schemas and parsed Perforce responses

The exact SDK and MCP library must be confirmed against current official documentation before implementation.

## Trust boundaries

1. ChatGPT must not receive unrestricted machine access.
2. The hosted gateway must not be trusted with raw Perforce passwords.
3. The local companion must reject tools and arguments outside its allowlist.
4. Perforce output is untrusted input and must be size-limited and parsed defensively.
5. Repository file contents may contain secrets and require explicit scope and limits.

## Read-only release tools

- `get_perforce_info`
- `get_opened_files`
- `get_pending_changelists`

Each tool should return:

- A structured success or error result
- The active server, user, and client where safe
- A warning when no client/workspace is configured
- No Perforce ticket, password, or environment secrets

## Write-operation policy

Write operations are out of scope for the initial milestone. Later write tools require:

- A narrow operation-specific schema
- A preview where Perforce supports it
- Explicit confirmation
- A human-readable action summary
- An audit record
- Clear recovery guidance

There will be no general-purpose `run_p4_command` or `run_shell_command` tool.