# Local Companion Specification

## Purpose
Bridge the hosted ChatGPT app to approved Perforce and Unreal tools on a user's workstation without exposing general remote-control capabilities.

## Initial platform
Windows 11, with architecture kept portable where practical.

## Form
A signed tray application plus background service. The tray UI handles pairing, status, workspace approval, confirmations, updates, and diagnostics. The service handles authenticated requests and bounded local operations.

## Responsibilities
- Discover and validate `p4`, P4V, UGS, Unreal installations
- Maintain an outbound encrypted connection to the hosted gateway
- Expose only allowlisted, versioned capabilities
- Store device keys in Windows secure storage
- Enforce local workspace/depot policies
- Run commands with explicit executable and argument arrays
- Display local confirmations when policy requires
- Stream bounded progress and final results
- Support immediate device revocation

## UI screens
- Welcome/install check
- Sign in and pair device
- Approved workspaces
- Connection/capabilities status
- Pending confirmations
- Recent operations/audit summary
- Settings and diagnostics
- Update available/required

## Service security
- No inbound public listening port by default
- Outbound authenticated channel only
- Device-specific asymmetric key pair
- Short-lived gateway credentials
- Request signatures, nonce, timestamp, and replay cache
- Capability and schema version negotiation
- Per-operation timeout and output limits
- No arbitrary process or path execution

## Workspace approval
Users select specific Perforce clients/workspaces. The companion records stable identifiers and approved root/depot scopes. Requests outside approved scope fail closed.

## Updates
- Signed packages
- Staged rollout
- Minimum supported version enforcement only for security-critical releases
- Rollback path
- Release notes and restart control

## Diagnostics bundle
User-triggered only. Redact tokens, tickets, passwords, source contents, and private server details by default. Show the bundle contents before export.
