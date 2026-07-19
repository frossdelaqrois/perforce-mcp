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

Phase 2 begins with `get_file_open_status`, which answers who has a specific
file or Unreal asset open and whether the visible open state blocks the current
user. It now also includes `explain_perforce_error`, a deterministic local
classifier for bounded, untrusted Perforce error text and known Companion error
codes.

## Project progress

GitHub phase-parent issues are the canonical source for progress and ordering.
The counts below are a linked snapshot of those issue checklists, last refreshed
on 19 July 2026; they are counts, not estimated completion percentages.

| Phase | Focus | Status | Checklist | Canonical source |
| --- | --- | --- | --- | --- |
| 0 | Product definition | Defined; reviews remain | — | Product documentation |
| 1 | Read-only local MCP prototype | Complete | — | [Milestone #1](https://github.com/frossdelaqrois/perforce-mcp/issues/1) |
| 2 | Unreal developer read workflows | Active | 2 / 8 | [Phase parent #46](https://github.com/frossdelaqrois/perforce-mcp/issues/46) |
| 3 | Trusted Windows companion | Planned | 0 / 8 | [Phase parent #48](https://github.com/frossdelaqrois/perforce-mcp/issues/48) |
| 4 | Safe confirmation-gated writes | Planned | 0 / 10 | [Phase parent #55](https://github.com/frossdelaqrois/perforce-mcp/issues/55) |
| 5 | Deeper Unreal Engine workflows | Planned | — | [`ROADMAP.md`](ROADMAP.md#phase-5--deeper-unreal-engine-workflows) |
| 6 | Hosted gateway and authentication | Planned | — | [`ROADMAP.md`](ROADMAP.md#phase-6--hosted-mcp-gateway-and-authentication) |
| 7 | ChatGPT app experience | Planned | — | [`ROADMAP.md`](ROADMAP.md#phase-7--chatgpt-app-experience) |
| 8 | Publication readiness | Planned | — | [`ROADMAP.md`](ROADMAP.md#phase-8--publication-readiness) |
| 9 | Expansion | Planned | — | [`ROADMAP.md`](ROADMAP.md#phase-9--expansion) |

### Current Sprint

- **Current phase:** Phase 2 — Unreal developer read workflows ([2 / 8 in #46](https://github.com/frossdelaqrois/perforce-mcp/issues/46)).
- **Next ordered feature:** [#40 — Summarise today's Perforce work](https://github.com/frossdelaqrois/perforce-mcp/issues/40).
- **Progress documentation:** [#57](https://github.com/frossdelaqrois/perforce-mcp/issues/57) keeps this snapshot and the roadmap aligned with GitHub.

### Recently Completed

- [#39 — Explain Perforce errors safely](https://github.com/frossdelaqrois/perforce-mcp/issues/39), merged in [PR #56](https://github.com/frossdelaqrois/perforce-mcp/pull/56).
- [#36 — Who has this asset open?](https://github.com/frossdelaqrois/perforce-mcp/issues/36), merged in [PR #38](https://github.com/frossdelaqrois/perforce-mcp/pull/38).

See [`ROADMAP.md`](ROADMAP.md) for the expanded phase checklists and the
documented synchronization workflow.

### Recommended Codex issue order

- [x] [**Issue #2** — Decide the .NET and MCP SDK versions](https://github.com/frossdelaqrois/perforce-mcp/issues/2)
- [x] [**Issue #3** — Scaffold the C# solution and tests](https://github.com/frossdelaqrois/perforce-mcp/issues/3)
- [x] [**Issue #4** — Discover and validate `p4`](https://github.com/frossdelaqrois/perforce-mcp/issues/4)
- [x] [**Issue #27** — Add GitHub Actions CI](https://github.com/frossdelaqrois/perforce-mcp/issues/27)
- [x] [**Issue #5** — Safe Perforce process runner and error model](https://github.com/frossdelaqrois/perforce-mcp/issues/5)
- [x] [**Issue #6** — `get_perforce_info`](https://github.com/frossdelaqrois/perforce-mcp/issues/6)
- [x] [**Issue #7** — `get_opened_files`](https://github.com/frossdelaqrois/perforce-mcp/issues/7)
- [x] [**Issue #8** — `get_pending_changelists`](https://github.com/frossdelaqrois/perforce-mcp/issues/8)

Check an item only after its pull request has been reviewed and merged into `main`.

## Build and test

From the repository root, run:

```powershell
dotnet restore PerforceMcp.slnx
dotnet build PerforceMcp.slnx --no-restore
dotnet test PerforceMcp.slnx --no-build
dotnet format PerforceMcp.slnx --verify-no-changes --no-restore
```

On Windows, `scripts\update-dev.ps1` reports the current branch, commit, version,
and working-tree state, then runs the complete restore, Release build, test, and
formatting workflow with a clear stage summary. It also detects changes to the
built MCP server DLLs, reminds you to restart Codex when necessary, and finishes
with a `Ready to test` status. The `.bat` wrapper is available for Command Prompt
users:

```powershell
.\scripts\update-dev.ps1
```

Pass `-Pull` to update the current branch first with a fast-forward-only pull.
Pulling is refused when the working tree has local changes:

```powershell
.\scripts\update-dev.ps1 -Pull
```

The batch wrapper forwards arguments, so Command Prompt users can run
`scripts\update-dev.bat -Pull`.

Use `scripts\clean.ps1` (or `clean.bat`) to remove generated `bin` and `obj`
directories beneath `src` and `tests`.

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

## `get_pending_changelists`

This read-only MCP tool lists pending changelists owned by the current Perforce user and workspace. It uses only tagged `info`, bounded `changes -s pending`, and bounded `opened -c` queries through the safe process runner. The default changelist appears only when it contains opened files and is explicitly identified by `id: "default"`, `number: null`, and `isDefault: true`; numbered changelists carry their numeric identifier, description, owner, client, pending status, and modified time.

The optional `limit` defaults to 20 and is capped at 100. File metadata is omitted unless `includeFiles` is true, while `fileLimit` independently defaults to 100 and is capped at 200 per changelist. `fileCount` reports the number safely inspected; `isFileCountExact` is false and `filesTruncated` is true when the per-changelist bound was reached. No raw command output or file contents are returned.

Empty results are successful. Invalid limits, missing login, missing workspace, unreachable server, timeout, malformed or oversized tagged output, and other non-zero exits return structured errors without echoing credentials, tickets, environment values, or raw output. The optional integration test is enabled by `PERFORCE_MCP_TEST_P4_PATH` and skipped by default.

## `get_file_open_status`

This Phase 2 read-only tool accepts one depot path, local path inside the active
workspace, or exact filename. Exact filenames use a bounded lookup and return
all bounded matches with ambiguity and truncation metadata; the tool never
silently chooses between duplicate names. Wildcards and local paths outside the
workspace are rejected.

Each match contains canonical depot and local paths, file type, Unreal-asset
classification for `.uasset` and `.umap`, and structured open records containing
the user, client, action, changelist, observed lock state, exclusive-open state,
whether the user and client match the current workspace, and whether that record
blocks the current workspace. Candidate and open-record
truncation are reported separately. A file opened by another
user—or by the current username in another client—is reported as blocking only
when the visible record uses an exclusive-open (`+l`) file type. A `locked` tag is
reported as raw observed state but does not independently prove that editing is
blocked. The tool never returns file contents or performs an
unlock, revert, checkout, sync, or other write operation.

The implementation uses only fixed, bounded tagged `info`, `files`, `fstat`, and
`opened -a` reads through the safe process runner. Its optional integration test
uses `PERFORCE_MCP_TEST_P4_PATH` and optionally `PERFORCE_MCP_TEST_FILE`; it is
skipped by default.

## `explain_perforce_error`

This Phase 2 read-only tool accepts optional `errorText` and `errorCode` fields;
at least one is required. `errorText` is limited to 4,096 characters and
`errorCode` to 64 characters. Recognized structured codes include the stable
codes already returned by Companion tools, such as `MissingLogin`,
`MissingClient`, `UnreachableServer`, and `TimedOut`, plus their documented
catalog forms such as `AUTH_REQUIRED` and `SERVER_UNREACHABLE`. Unknown supplied
codes and oversized or empty requests return structured request errors.

The classifier is deterministic, local, and rule based. It does not call an AI
model, execute `p4` or another process, read files, or inspect environment
variables. It never returns the supplied error text. Instead, it returns a
normalized category and code, a short summary, observed facts, separately
labelled possible causes, safe verification steps, confidence and ambiguity
indicators, and bounded redaction indicators. Authentication, connection,
workspace, permission, resolve, exclusive-lock, storage, temporary server, and
unknown or ambiguous failures are covered initially.

Credential-like values, embedded credentials, sensitive command arguments,
username-like values, and local paths are detected and omitted. Instruction-like
or fake MCP content is treated only as untrusted data and is never followed. A
safe response is shaped like this abbreviated example:

```json
{
  "category": "authentication",
  "normalizedCode": "AUTH_REQUIRED",
  "summary": "Perforce authentication is required or may have expired.",
  "observedFacts": ["A recognized structured error code identified a specific category."],
  "possibleCauses": ["The login session may be missing or expired."],
  "safeNextSteps": ["Verify login status using an approved read-only connection check."],
  "confidence": "High",
  "isAmbiguous": false,
  "redactionOccurred": false
}
```

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
- Phase 1 read-only local prototype: complete
- Phase 2 Unreal developer read workflows: active at 2 / 8
- Next ordered feature: [#40 — Summarise today's Perforce work](https://github.com/frossdelaqrois/perforce-mcp/issues/40)
- Hosted, companion, write, and ChatGPT app work: specified as later phases

Public APIs and architecture may still change during the prototype stage.

## Important boundaries

This project is not currently affiliated with or endorsed by Perforce Software, Epic Games, or OpenAI. Product naming and trademark use must be reviewed before public release.

## Licence

A licence has not yet been selected. Choose and add an appropriate open-source licence before accepting external contributions.
