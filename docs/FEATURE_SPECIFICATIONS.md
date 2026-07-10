# Core Feature Specifications

## 1. Find who has an Unreal asset locked

### User intent
Identify whether a `.uasset` or `.umap` is blocked and who currently holds the relevant open/lock.

### Inputs
- Depot path, local path, exact filename, or bounded search term
- Optional workspace/device when more than one is connected

### Flow
1. Resolve the file within approved depot/workspace scope.
2. Fetch metadata, opened records, exclusive-lock state, and changelist details.
3. Return all exact matches or ask the user to select when ambiguous.
4. Explain whether the state actually blocks editing.

### Result
- Depot path and asset name
- Open/lock state
- User and workspace, where permitted
- Changelist and description
- Recommended safe next step

### Errors
File not found, multiple matches, permission denied, companion offline, server unavailable.

## 2. Summarise a pending changelist

### User intent
Understand what a changelist contains before shelving or submitting it.

### Flow
1. Verify changelist exists and is pending.
2. Read file list and metadata.
3. Group files by action, type, module/folder, and Unreal asset category.
4. Include bounded text diffs where explicitly permitted.
5. Detect warnings: unresolved files, opened elsewhere, missing lock, very large change, empty description.

### Result
- Plain-language summary
- Counts by action/type
- Important files
- Risk/warning section
- Suggested description, clearly marked as generated

The read-only summary tool must not update the description automatically.

## 3. Explain a Perforce error

### User intent
Translate a P4V, UGS, Unreal, or `p4` failure into understandable recovery steps.

### Flow
1. Accept pasted error text or a selected recent local operation.
2. Redact tickets, tokens, usernames/paths where policy requires.
3. Match stable known error categories.
4. Present the likely meaning, evidence, and safe checks.
5. Clearly label uncertain diagnosis.

Never recommend destructive commands as the first step.

## 4. Show current workspace status

### Result sections
- Connection and login
- Workspace identity and stream
- Have/sync status when available
- Opened files and pending changelists
- Locks/blockers
- UGS build/sync state
- Warnings and next actions

The query must be bounded and should avoid expensive full-workspace scans by default.

## 5. Review latest UGS build failure

### Flow
1. Identify configured project and latest relevant build.
2. Read result metadata and bounded log excerpt.
3. Extract the first actionable compiler/build error and affected module/file.
4. Distinguish root error from cascading failures.
5. Suggest a next diagnostic action.

### Privacy
Logs may contain paths, usernames, server names, code, or secrets. Apply local redaction and strict output limits.

## 6. Sync workspace (later phase)

### Required flow
1. Resolve exact device, server, workspace, and target changelist.
2. Run preview.
3. Detect clobber risks, writable files, unresolved files, and disk-space concerns.
4. Present confirmation summary.
5. Revalidate state.
6. Execute with progress and cancellation where safe.
7. Return exact final changelist and partial failures.

Never interpret "sync latest" across an unspecified workspace when multiple workspaces are available.

## 7. Submit changelist (later phase)

### Required preconditions
- Numbered pending changelist owned by current user
- No unresolved files
- Required locks held
- Description passes minimum policy
- Preview/current state has not changed
- Enhanced confirmation completed

### Result
Return submitted changelist number, file count, warnings, and any triggered build status. Never claim success based only on process start.
