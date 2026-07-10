# Product Requirements Document

## Product
Perforce Companion

## Objective
Deliver a publishable ChatGPT app that safely connects users to approved Perforce workspaces, with strong support for Unreal Engine and UnrealGameSync workflows.

## Primary users
- Solo and small-team Unreal developers
- Game studios using Helix Core
- Technical artists and designers using P4V
- Build engineers and leads using UGS

## Core problems
- Perforce status is difficult to inspect conversationally.
- File locks and changelists are hard for non-specialists to understand.
- AI tools lack safe, narrow Perforce access.
- Hosted AI cannot directly access private workstations and studio networks.

## V1 scope
- Local read-only MCP server
- Safe `p4` discovery and execution
- Workspace information
- Opened files
- Pending changelists
- Clear setup and diagnostics

## Public app scope
- Hosted authenticated MCP gateway
- Signed local Windows companion
- Device pairing and revocation
- Workspace, file, lock, history, and changelist reads
- Unreal asset lock awareness
- UGS build-status reads
- Native ChatGPT cards and onboarding

## Later scope
- Sync, checkout, changelist creation, shelving, and submission
- Studio policies and enterprise identity
- Multiple devices and workspaces
- Codex and other MCP clients

## Non-goals
- Replacing P4V
- General remote administration
- Arbitrary shell or filesystem access
- Perforce superuser administration
- Automatic destructive actions

## Success metrics
- New user connects a workspace without manually editing MCP JSON.
- Read-only results match Perforce source data.
- Tool-selection evaluations meet release threshold.
- No secrets appear in normal outputs or logs.
- Write operations have zero confirmed bypasses in security tests.
- App is accepted into the ChatGPT app directory.

## V1 release criteria
- Three read-only tools implemented and tested
- Windows setup documented
- Stable structured errors
- No generic command execution surface
- Unit tests and optional integration tests pass
- Security review of process execution and redaction complete

## Product principles
- Useful before write access
- Least privilege
- Local credentials stay local
- Preview before impact
- Clear human confirmation
- Explain facts separately from AI inference
