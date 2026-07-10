# Permissions and Confirmations

## Permission tiers

### Tier 0 — disconnected
No Perforce or device data is available.

### Tier 1 — connection metadata
May report paired devices, online state, and installed capabilities. No depot or workspace content.

### Tier 2 — read-only workspace access
May read approved workspace metadata, changelists, file status, locks, history, bounded diffs, and UGS status.

### Tier 3 — routine write access
May sync, open files, create/update changelists, shelve, and unshelve after tool-specific checks and confirmation where required.

### Tier 4 — high-impact write access
May submit, revert local changes, delete, unlock, force-sync, or resolve conflicts. Always requires enhanced confirmation and may be disabled by studio policy.

### Tier 5 — administration
Out of scope. The app does not expose Perforce superuser administration.

## Scope dimensions

Permissions should be enforceable by:

- User/account
- Organisation or studio
- Device
- Perforce server
- Workspace
- Depot/stream path
- Tool/action
- Time window

## Confirmation classes

### No confirmation
Pure read-only operations with bounded data access.

### Standard confirmation
Routine reversible changes, such as creating a changelist or opening files for edit.

Confirmation displays:
- Action
- Server/workspace
- File count and scope
- Destination changelist, where relevant

### Enhanced confirmation
Shared, destructive, difficult-to-reverse, or conflict-prone operations.

Required for:
- Submit
- Revert files containing local modifications
- Open for delete
- Force sync
- Unlock another user's file
- Resolve with automatic or overwrite strategies

Confirmation displays:
- Exact operation and target
- Preview/diff summary
- Number and type of affected files
- Shared repository impact
- Recovery limitations
- Explicit Confirm and Cancel controls

## Revalidation

Before executing any confirmed write operation, the companion must re-check:

- Device and user session
- Active server and workspace
- Ownership/permission
- Changelist status
- File state and lock state
- Whether the preview is stale

Material changes invalidate the confirmation and require a new preview.

## Studio policies

Administrators should eventually be able to:

- Disable all writes
- Disable individual high-impact tools
- Limit approved depots/streams
- Require local confirmation in addition to ChatGPT confirmation
- Require ticket/SSO reauthentication
- Set maximum file/result sizes
- Configure audit retention
- Disable sending file contents or diffs to hosted services

## Audit records

Record:

- Request ID
- User, device, server, workspace
- Tool and redacted inputs
- Preview hash/version
- Confirmation event
- Start/end status and duration
- Perforce changelist/result identifiers

Do not record passwords, tickets, tokens, full source contents, or unrestricted command output.

## Failure policy

- Fail closed when identity, scope, permission, or confirmation is uncertain.
- Never silently broaden a path or file selection.
- Never retry a non-idempotent write automatically unless the operation has a safe idempotency key.
- Report partial completion explicitly.
