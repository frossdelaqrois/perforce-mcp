# Perforce Companion for ChatGPT

A planned ChatGPT app that lets developers safely inspect and work with Perforce repositories through natural language.

The long-term goal is publication in the ChatGPT app directory as a Perforce counterpart to the GitHub app. The MCP server is the first technical foundation; the complete product also requires a secure local companion, hosted gateway, authentication, ChatGPT user interface, and production operations.

## Intended users

- Unreal Engine teams using P4V and UnrealGameSync
- Game studios using Perforce Helix Core
- Solo developers who want simpler Perforce workflows
- Developers using ChatGPT, Codex, or another MCP-compatible client

## Example requests

- "Show the files I have checked out."
- "Who has this Unreal map locked?"
- "Summarise my pending changelist."
- "Explain this Perforce error."
- "Show the latest UnrealGameSync build status."
- "Shelve these files for review." — later phase, with confirmation

## Planned architecture

```text
ChatGPT app / future MCP clients
              |
              v
Hosted authenticated MCP gateway
              |
              v
Secure local Windows companion
              |
              +-- p4.exe / P4V
              +-- UnrealGameSync
              +-- Unreal Engine workspaces
```

A hosted service cannot directly run commands on a developer's PC or private studio network. The local companion executes only narrowly defined, allowlisted operations. High-impact actions require previews, revalidation, and explicit confirmation.

## Technology baseline

- **.NET 10 LTS**
- Official **Model Context Protocol C# SDK**
- Initial package: `ModelContextProtocol` **1.4.1**
- Windows 11 as the first local-companion platform
- `p4` command-line client as the automation source of truth
- P4V and UnrealGameSync integrations remain optional additions

The decision is recorded in [`docs/decisions/0001-dotnet-and-mcp-sdk.md`](docs/decisions/0001-dotnet-and-mcp-sdk.md).

## Safety principles

- Read-only tools first
- No arbitrary shell-command or generic raw-P4 tool
- Direct executable invocation with argument arrays
- Strict timeouts, cancellation, and output limits
- Explicit confirmation for submit, revert, unlock, force-sync, delete, and risky resolves
- Least-privilege Perforce credentials
- Secrets remain local where possible
- Structured, redacted tool responses
- Repository content and build logs are treated as untrusted input
- Clear audit records without passwords, tickets, tokens, or source contents

## Current milestone

Build a local read-only MCP prototype exposing:

- `get_perforce_info`
- `get_opened_files`
- `get_pending_changelists`

### Recommended Codex issue order

- [x] **Issue #2** — Decide the .NET and MCP SDK versions
- [x] **Issue #3** — Scaffold the C# solution and tests
- [x] **Issue #4** — Discover and validate `p4`
- [x] **Issue #27** — Add GitHub Actions CI
- [x] **Issue #5** — Safe Perforce process runner and error model
- [x] **Issue #6** — `get_perforce_info`
- [ ] **Issue #7** — `get_opened_files`
- [ ] **Issue #8** — `get_pending_changelists`

Check an item only after its pull request has been reviewed and merged into `main`.

## Build and test

From the repository root, run:

```powershell
dotnet restore PerforceMcp.slnx
dotnet build PerforceMcp.slnx --no-restore
dotnet test PerforceMcp.slnx --no-build
dotnet format PerforceMcp.slnx --verify-no-changes --no-restore
```

GitHub Actions runs the same four commands for pull requests and pushes to `main`. NuGet lock files are committed so dependency caching is keyed to the complete resolved package graph.

## Perforce process runner

The Perforce adapter contains an internal process runner constructed from a successfully validated executable result. It invokes that absolute executable directly, passes arguments through `ProcessStartInfo.ArgumentList`, applies cancellation and timeouts with bounded cleanup, and captures at most 32 KiB from each output stream. Results include the exit code, duration, truncation flags, redacted output, and a stable structured error when execution fails.

The runner is not an MCP tool and does not accept shell command strings. Callers must provide known argument lists for narrowly defined Perforce operations. Passwords, tickets, and caller-supplied sensitive values are redacted from captured output.

## `get_perforce_info`

The first read-only MCP tool runs only `p4 -ztag info` through the safe process runner. It returns the server address, current user, client/workspace, client root, and server version as structured fields. It reports missing login, missing workspace, unreachable server, malformed output, and timeout conditions without returning passwords, tickets, raw environment data, or raw command output.

Set `PERFORCE_MCP_P4_PATH` to an absolute `p4` or `p4.exe` path to override PATH discovery when starting the stdio server. The optional integration test is skipped unless `PERFORCE_MCP_TEST_P4_PATH` contains an absolute path to a test `p4` executable configured for a disposable test server.

## `get_opened_files`

This read-only MCP tool runs only tagged `p4 opened` through the safe process runner. It returns a structured list containing each depot path, local path when Perforce supplies one, open action, changelist, file type, observed lock state, and whether the file type requires exclusive open. It never returns raw `p4 opened` output or file contents.

The optional `limit` is 50 by default and must be from 1 to 200. The adapter asks Perforce for one extra record so `isTruncated` reliably indicates that more results exist. The optional `changelist` filter accepts `default` or a positive pending changelist number; other values are rejected before `p4` runs. Empty workspaces return a successful empty list. Missing login, missing workspace, unreachable server, timeout, malformed or oversized tagged output, and other non-zero exits return stable structured errors without echoing raw output.

The optional integration test uses the same `PERFORCE_MCP_TEST_P4_PATH` setting as `get_perforce_info` and is safe against a disposable or normal workspace because it only reads opened-file metadata.

## Delivery phases

1. Read-only local MCP prototype
2. Expanded Perforce read capabilities
3. Secure Windows companion
4. Hosted authenticated gateway and device pairing
5. UnrealGameSync and Unreal-specific features
6. ChatGPT Apps SDK onboarding and result cards
7. Safe, confirmation-gated write operations
8. Privacy, security, reliability, and support readiness
9. ChatGPT app-directory submission
10. Optional Codex, enterprise, macOS, Linux, and other MCP-client support

## Documentation

### Product and roadmap

- [`ROADMAP.md`](ROADMAP.md) — phased delivery plan
- [`docs/PRD.md`](docs/PRD.md) — product requirements
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — product definition
- [`docs/BRANDING.md`](docs/BRANDING.md) — working name and listing direction

### Architecture and implementation

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — system structure and trust boundaries
- [`docs/MCP_TOOL_CATALOG.md`](docs/MCP_TOOL_CATALOG.md) — staged public tool catalog
- [`docs/LOCAL_COMPANION.md`](docs/LOCAL_COMPANION.md) — Windows companion design
- [`docs/GATEWAY_AND_PAIRING.md`](docs/GATEWAY_AND_PAIRING.md) — hosted routing and device pairing
- [`docs/UGS_AND_P4V_INTEGRATION.md`](docs/UGS_AND_P4V_INTEGRATION.md) — UnrealGameSync and P4V plan
- [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md) — development setup outline
- [`AGENTS.md`](AGENTS.md) — rules for Codex and other coding agents

### UX, safety, and testing

- [`docs/CHATGPT_UX.md`](docs/CHATGPT_UX.md) — ChatGPT onboarding, cards, and states
- [`docs/FEATURE_SPECIFICATIONS.md`](docs/FEATURE_SPECIFICATIONS.md) — core workflow behaviour
- [`docs/PERMISSIONS_AND_CONFIRMATIONS.md`](docs/PERMISSIONS_AND_CONFIRMATIONS.md) — permission tiers and confirmations
- [`docs/SECURITY.md`](docs/SECURITY.md) — initial security model
- [`docs/EVALUATION_PLAN.md`](docs/EVALUATION_PLAN.md) — tool-selection and red-team tests

### Operations and publication

- [`docs/RELEASE_OPERATIONS.md`](docs/RELEASE_OPERATIONS.md) — CI, deployment, monitoring, rollback, and incidents
- [`docs/CHATGPT_SUBMISSION_CHECKLIST.md`](docs/CHATGPT_SUBMISSION_CHECKLIST.md) — publication readiness
- [`docs/PRIVACY_POLICY_TEMPLATE.md`](docs/PRIVACY_POLICY_TEMPLATE.md) — draft legal template, not final legal advice
- [`docs/TERMS_TEMPLATE.md`](docs/TERMS_TEMPLATE.md) — draft legal template, not final legal advice

## Repository status

- Product planning and architecture: substantially defined
- Initial .NET solution scaffold: complete
- Technology decision: complete
- `p4` discovery implementation: under review in PR #28
- Read-only implementation issues: sequenced for Codex
- Hosted and ChatGPT app work: specified as later issues

Public APIs and architecture may still change during the prototype stage.

## Important boundaries

This project is not currently affiliated with or endorsed by Perforce Software, Epic Games, or OpenAI. Product naming and trademark use must be reviewed before public release.

## Licence

A licence has not yet been selected. Choose and add an appropriate open-source licence before accepting external contributions.
