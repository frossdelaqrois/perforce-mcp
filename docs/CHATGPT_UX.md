# ChatGPT App UX Specification

## Experience goal

The app should feel like a native ChatGPT integration, not a remote terminal. Users ask ordinary questions; the app presents concise answers, structured cards, clear connection state, and explicit confirmation for consequential actions.

## Primary user journeys

### First connection

1. User selects the Perforce plugin in ChatGPT.
2. App explains that a local companion is required because Perforce normally runs on a private workstation or studio network.
3. User signs in to the hosted service.
4. User installs the signed Windows companion.
5. Companion displays a short pairing code or opens a secure browser flow.
6. User selects an approved Perforce workspace.
7. App verifies read-only connectivity and shows a connection summary.

Success screen:

```text
Perforce connected
Device: DANIEL-PC
Workspace: Poker-UE58
Server: connected
Capabilities: P4V, p4, UnrealGameSync
Mode: Read-only
```

### Workspace overview

Prompt examples:

- "What is my Perforce status?"
- "Show what I have checked out."
- "Do I have anything waiting to submit?"

Overview card fields:

- Device online/offline
- Server connection
- Active workspace
- Current user
- Opened file count
- Pending changelist count
- Locked Unreal asset count
- UGS status, when configured

### Opened files

Display groups by changelist. Each row should show filename, action, type, lock state, and shortened depot path. Binary Unreal files should be marked as non-text-diffable.

Actions in later phases:

- View changelist
- Move to changelist
- Revert selected files
- Open in P4V

### File lock result

For a question such as "Who has MainLevel.umap locked?", show:

- Asset name and depot path
- Lock/open state
- User and workspace, subject to permissions
- Changelist number and description
- Last activity time if available
- Suggested non-destructive next steps

Never offer a force unlock unless the current user has authority and explicitly asks.

### Changelist summary

Show:

- Number and description
- Owner and workspace
- File counts by action and type
- Unreal asset count
- Text diff summary when available
- Warnings: large files, binaries without locks, unresolved files, or files opened elsewhere

Later write actions:

- Improve description
- Shelve
- Submit

Submit must always trigger a final confirmation card containing the exact changelist, file count, server, workspace, and warning that submission changes shared repository history.

### Build failure

Show a concise failure card:

- Build name and changelist
- Failed stage
- First actionable error
- Affected project/module
- Link or local action to open full log
- Suggested next diagnostic step

The model may explain logs but must clearly distinguish extracted facts from inferred causes.

## Component inventory

### Connection card

States: connected, companion offline, authentication required, Perforce login required, workspace missing, unsupported version.

### Workspace status card

Compact summary suitable for repeated use.

### File table

Paginated or truncated. Never attempt to render thousands of rows.

### Changelist card

Supports read-only summary and later action buttons.

### Lock alert card

Highlights blocking users/workspaces and safe resolution guidance.

### Confirmation card

Required for write operations. Contains action, scope, impact, target server/workspace, preview result, and Cancel/Confirm controls.

### Operation progress card

Shows queued, running, completed, cancelled, or failed. Long operations must expose cancellation when safe.

## Conversational behaviour

- Ask only for missing information that cannot be inferred safely.
- Prefer the active paired device and workspace, but state which ones are being used.
- When multiple devices/workspaces match, present a short selector.
- Do not claim an action succeeded until the local companion reports completion.
- Explain Perforce errors in plain language while preserving the original error code/message in a details section.
- Do not overwhelm users with raw command output.

## Mobile considerations

- Cards must work in narrow layouts.
- Critical fields appear before tables.
- Use expandable details for depot paths and logs.
- Confirmation controls remain visible without horizontal scrolling.

## Accessibility

- Do not rely on colour alone for status.
- Provide text labels for icons.
- Ensure logical keyboard and screen-reader order.
- Use plain error language and clear recovery actions.
- Avoid rapidly updating progress text.

## Empty and failure states

Every state must suggest a safe next step:

- No opened files: "This workspace has no files open."
- Companion offline: "Open Perforce Companion on DANIEL-PC or choose another device."
- Login expired: "Sign in to Perforce on the local device. Your password is not sent to ChatGPT."
- No UGS configuration: "UGS is not configured for this workspace; Perforce tools still work."

## App metadata draft

Working name: **Perforce Companion**

One-line description: **Safely inspect Perforce workspaces, changelists, file locks, and UnrealGameSync status from ChatGPT.**

Suggested conversation starters:

- Show my opened Perforce files.
- Who has this Unreal asset locked?
- Summarise my pending changelists.
- What failed in the latest UGS build?
