# Product Roadmap

## Product objective

Publish a trustworthy Perforce integration in the ChatGPT app directory. It should provide a simple, safe experience comparable to connecting GitHub, while supporting the local realities of P4V, UnrealGameSync, and developer workspaces.

## Phase 0 — Product definition

- Define target users and supported workflows
- Choose the public product name
- Document threat model and trust boundaries
- Decide initial supported operating systems
- Confirm Perforce licensing and branding requirements

**Exit:** The product scope, safety rules, and first release criteria are documented.

## Phase 1 — Read-only local MCP prototype

- Create the MCP server project
- Locate and validate the `p4` executable
- Add GitHub Actions checks for restore, build, tests, and formatting
- Run commands without invoking a shell
- Parse tagged or JSON-compatible Perforce output
- Add `get_perforce_info`
- Add `get_opened_files`
- Add `get_pending_changelists`
- Add errors, timeouts, tests, and setup documentation

**Exit:** An MCP client can inspect a configured local Perforce workspace without changing it.

## Phase 2 — Perforce read capabilities

- **Who has this asset open?** Identify locks and users holding files.
- **Explain my Perforce error.** Convert safe, redacted diagnostics into actionable guidance.
- **Summarise today's work.** Group the current user's opened files and changelists.
- **Show only Unreal assets I'm editing.** Filter `.uasset` and `.umap` work clearly.
- **Find files blocking my sync.** Surface relevant workspace and lock metadata.
- **Recent submissions affecting my workspace.** Read bounded file history and changelists.
- Add supporting workspace, depot search, diff, stream, and branch reads only as these workflows require them.

**Exit:** The app covers common investigation and status questions.

## Phase 3 — Windows companion, P4V, and UnrealGameSync

- Build a signed Windows companion service or tray app
- Discover P4V and UnrealGameSync installations
- Open changelists in P4V and files in Unreal Editor
- Launch UnrealGameSync and expose narrow, previewable sync-to-good-build workflows
- Pair a user device with the hosted gateway
- Store tokens in Windows Credential Manager
- Add automatic updates and revocation
- Show every requested operation locally

**Exit:** A hosted ChatGPT app can securely request narrow actions on an authorised PC.

## Phase 4 — Safe write operations

- Sync preview and sync
- Open files for edit/add/delete
- Create and update changelists
- Shelve and unshelve
- Resolve previews
- Submit with mandatory confirmation
- Revert, force-sync, unlock, and delete with enhanced warnings

**Exit:** Common write workflows are reliable, auditable, and confirmation-gated.

## Phase 5 — Deeper Unreal Engine workflows

- Extend UGS project and build-status support beyond the Phase 3 launch integrations
- Explain build and compile failures
- Detect checked-out `.uasset` and `.umap` files
- Improve lock and exclusive-edit guidance

**Exit:** The app provides clear value to Unreal Engine teams.

## Phase 6 — Hosted MCP gateway and authentication

- Implement account authentication
- Implement encrypted device pairing
- Route tool calls to the correct authorised device
- Add tenant isolation, rate limits, logs, and abuse controls
- Add availability monitoring and incident procedures

**Exit:** Multiple users can connect safely without sharing machines or credentials.

## Phase 7 — ChatGPT app experience

- Implement Apps SDK integration
- Design cards for files, locks, changelists, and builds
- Add clear confirmation UI for write actions
- Add onboarding, reconnect, and error-recovery flows
- Test tool names and descriptions for accurate model selection

**Exit:** The complete workflow is usable from ChatGPT without technical setup beyond installation and sign-in.

## Phase 8 — Publication readiness

- Privacy policy
- Terms of service
- Support and status pages
- Data retention and deletion controls
- Security review and penetration testing
- Accessibility and usability review
- App metadata, screenshots, icon, and listing copy
- Submission and reviewer feedback fixes

**Exit:** The app satisfies the current ChatGPT app-directory requirements and is submitted.

## Phase 9 — Expansion

- Codex support
- Other MCP-compatible clients
- macOS/Linux companion where practical
- Enterprise deployment controls
- Self-hosted gateway option
- Studio policy packs and custom approval rules
