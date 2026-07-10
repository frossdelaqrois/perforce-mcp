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

### Recommended Codex order

1. **Issue #3** — Scaffold the C# solution and tests
2. **Issue #4** — Discover and validate `p4`
3. **Issue #5** — Safe Perforce process runner and error model
4. **Issue #6** — `get_perforce_info`
5. **Issue #7** — `get_opened_files`
6. **Issue #8** — `get_pending_changelists`

Issue #2, the technology decision, is complete.

## Build and test

From the repository root, run:

```powershell
dotnet restore PerforceMcp.slnx
dotnet build PerforceMcp.slnx --no-restore
dotnet test PerforceMcp.slnx --no-build
```

The current scaffold starts no MCP server and runs no Perforce commands. Server hosting and read-only tools are added by later milestone issues.

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
- Initial implementation: not yet scaffolded
- First technology decision: complete
- Read-only implementation issues: ready for Codex
- Hosted and ChatGPT app work: specified as later issues

Public APIs and architecture may still change during the prototype stage.

## Important boundaries

This project is not currently affiliated with or endorsed by Perforce Software, Epic Games, or OpenAI. Product naming and trademark use must be reviewed before public release.

## Licence

A licence has not yet been selected. Choose and add an appropriate open-source licence before accepting external contributions.
