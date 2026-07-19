# Product Roadmap

## Product objective

Publish a trustworthy Perforce integration in the ChatGPT app directory. It should provide a simple, safe experience comparable to connecting GitHub, while supporting the local realities of P4V, UnrealGameSync, and developer workspaces.

## Progress source of truth

GitHub phase-parent issues are the canonical source for phase progress and work
ordering. This roadmap explains the phases and links to those issues; the
[`README.md`](README.md#project-progress) contains only a concise linked
snapshot. Checklist counts in both documents are copied from the canonical
parent issues at update time and are never estimated percentages.

Current canonical parents:

- Phase 2: [#46 — Unreal developer read workflows](https://github.com/frossdelaqrois/perforce-mcp/issues/46) (`2 / 8`)
- Phase 3: [#48 — Trusted Windows companion](https://github.com/frossdelaqrois/perforce-mcp/issues/48) (`0 / 8`)
- Phase 4: [#55 — Safe confirmation-gated write operations](https://github.com/frossdelaqrois/perforce-mcp/issues/55) (`0 / 10`)

Snapshot refreshed: 19 July 2026.

## Phase 0 — Product definition

Establish the audience, supported workflows, trust boundaries, initial platform,
brand direction, and public-release constraints before expanding implementation.

- Define target users and supported workflows
- Choose the public product name
- Document threat model and trust boundaries
- Decide initial supported operating systems
- Confirm Perforce licensing and branding requirements

**Exit:** The product scope, safety rules, and first release criteria are documented.

## Phase 1 — Read-only local MCP prototype

Deliver the smallest useful local foundation: fixed Perforce reads, direct and
bounded process execution, stable structured responses, tests, CI, and setup
documentation. [Milestone #1](https://github.com/frossdelaqrois/perforce-mcp/issues/1)
is closed as completed.

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

## Phase 2 — Unreal developer read workflows

Turn the read-only foundation into bounded workflows that answer common Unreal
developer questions. The canonical checklist and ordering live in
[#46](https://github.com/frossdelaqrois/perforce-mcp/issues/46): `2 / 8`.

- [x] [#36 — Who has this asset open?](https://github.com/frossdelaqrois/perforce-mcp/issues/36)
- [x] [#39 — Explain Perforce errors safely](https://github.com/frossdelaqrois/perforce-mcp/issues/39)
- [ ] [#40 — Summarise today's Perforce work](https://github.com/frossdelaqrois/perforce-mcp/issues/40)
- [ ] [#41 — Show only Unreal assets being edited](https://github.com/frossdelaqrois/perforce-mcp/issues/41)
- [ ] [#42 — Show recent submissions affecting the workspace](https://github.com/frossdelaqrois/perforce-mcp/issues/42)
- [ ] [#43 — Find files blocking a workspace sync](https://github.com/frossdelaqrois/perforce-mcp/issues/43)
- [ ] [#44 — Add workspace health summary](https://github.com/frossdelaqrois/perforce-mcp/issues/44)
- [ ] [#45 — Complete Phase 2 manual validation and release closeout](https://github.com/frossdelaqrois/perforce-mcp/issues/45)

**Exit:** Common investigation and status workflows pass live validation, the
documentation is current, and a reviewed Phase 2 release tag identifies the
merged release.

## Phase 3 — Trusted Windows companion

Build the trusted local boundary for pairing, visibility, confirmation,
revocation, P4V/Unreal launches, and future write operations. The canonical
checklist lives in [#48](https://github.com/frossdelaqrois/perforce-mcp/issues/48):
`0 / 8`.

- [ ] [#18 — Local Windows companion shell](https://github.com/frossdelaqrois/perforce-mcp/issues/18)
- [ ] [#19 — Secure device pairing and revocation](https://github.com/frossdelaqrois/perforce-mcp/issues/19)
- [ ] [#22 — UnrealGameSync detection and build-status reads](https://github.com/frossdelaqrois/perforce-mcp/issues/22)
- [ ] [#23 — Signed installer and secure update channel](https://github.com/frossdelaqrois/perforce-mcp/issues/23)
- [ ] [#47 — Local confirmation and audit framework](https://github.com/frossdelaqrois/perforce-mcp/issues/47)
- [ ] Detect P4V and supported Unreal installations
- [ ] Open approved changelists in P4V and files or projects in Unreal
- [ ] Complete companion threat-model review and live validation

**Exit:** The companion can be installed, paired, stopped, disconnected,
revoked, and updated safely; read-only tools work through it; and no write
operation is enabled before review and closeout.

## Phase 4 — Safe confirmation-gated write operations

Add narrowly scoped writes only through the trusted companion, with read-only
previews, expiring request-bound local confirmation, revalidation, bounded
auditing, and honest partial-failure reporting. Phase 3 must complete first. The
canonical checklist lives in [#55](https://github.com/frossdelaqrois/perforce-mcp/issues/55):
`0 / 10`.

- [ ] [#49 — Previewable workspace sync operations](https://github.com/frossdelaqrois/perforce-mcp/issues/49)
- [ ] [#50 — Safe file open and revert operations](https://github.com/frossdelaqrois/perforce-mcp/issues/50)
- [ ] [#51 — Confirmed changelist management operations](https://github.com/frossdelaqrois/perforce-mcp/issues/51)
- [ ] [#52 — Confirmed shelving operations](https://github.com/frossdelaqrois/perforce-mcp/issues/52)
- [ ] [#54 — Resolve previews and safe conflict handling](https://github.com/frossdelaqrois/perforce-mcp/issues/54)
- [ ] [#53 — Preview, validation, and confirmed submit](https://github.com/frossdelaqrois/perforce-mcp/issues/53)
- [ ] Complete the write-operation security and red-team evaluation
- [ ] Pass disposable live tests without disturbing unrelated work
- [ ] Update documentation, confirmations, audit behavior, and recovery guidance
- [ ] Create a reviewed Phase 4 release tag from the exact merged commit

**Exit:** Common workspace, changelist, shelving, resolve, and submit operations
are reliable, auditable, recoverable, and confirmation-gated.

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

## Synchronization workflow

Update progress documentation only after the feature state is settled on GitHub:

1. Merge the active feature pull request.
2. Update local `main` from the remote.
3. Confirm the feature issue is closed as completed.
4. Mark the feature complete in its canonical phase-parent issue.
5. Create a fresh documentation branch from updated `main`.
6. Read every canonical parent issue and calculate the checked / total counts.
7. Update `README.md` and `ROADMAP.md` together, including the refresh date.
8. Verify Markdown links and formatting.
9. Open a separate documentation-only draft pull request.

When GitHub and these files disagree, GitHub wins. Do not infer progress from
code presence, open pull requests, or subjective estimates, and do not use
completion percentages.
