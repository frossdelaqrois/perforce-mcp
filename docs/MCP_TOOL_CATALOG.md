# MCP Tool Catalog

This catalog defines the planned public tools for the Perforce ChatGPT app. Tools are grouped by release phase so implementation stays narrow and reviewable.

## Tool design rules

- Use verb-first, unambiguous names.
- One tool performs one operation.
- Read tools must not modify Perforce state.
- Write tools require operation-specific schemas and confirmation metadata.
- Never expose `run_shell_command` or generic `run_p4_command`.
- Inputs must be bounded and validated.
- Outputs must be structured, truncated where necessary, and free of secrets.
- Descriptions must say when the tool should and should not be used.

## Shared response envelope

Every tool should return:

```json
{
  "ok": true,
  "data": {},
  "warnings": [],
  "truncated": false,
  "requestId": "opaque-id"
}
```

Errors should use stable codes such as `P4_NOT_FOUND`, `AUTH_REQUIRED`, `CLIENT_NOT_CONFIGURED`, `SERVER_UNREACHABLE`, `TIMEOUT`, `PERMISSION_DENIED`, `RESULT_LIMIT_EXCEEDED`, and `CONFIRMATION_REQUIRED`.

# Phase 1: read-only prototype

## `get_perforce_info`

Purpose: Report the active server, user, workspace, root, and server version.

Input: none.

Do not use for: credential retrieval or server administration.

## `get_opened_files`

Purpose: List files opened in the current workspace.

Input:

```json
{ "changelist": "12345", "limit": 50 }
```

Both fields are optional. `changelist` accepts a positive number or `default`; `limit` defaults to 50 and is capped at 200. Return depot path, local path when Perforce supplies one, action, changelist, file type, observed lock state, and exclusive-open state. Indicate truncation and never return raw command output.

## `get_pending_changelists`

Purpose: List pending changelists for the current user/workspace.

Input:

```json
{ "limit": 20, "includeFiles": false, "fileLimit": 100 }
```

All fields are optional. `limit` is capped at 100 changelists and `fileLimit` at 200 files per changelist. Return a clearly identified default changelist when it contains opened files plus numbered pending changelists for the current user/workspace. Each result contains description, owner, client, status, modified time when available, bounded file count with exactness metadata, optional structured files, and truncation state. Never return raw command output or file contents.

# Phase 2: expanded read access

## `get_file_open_status`

Resolve one depot path, in-workspace local path, or exact filename and return a
bounded structured list of matches. Report open user, client, action, changelist,
file type, lock and exclusive-open state, whether the opener is the current user,
and whether the visible state actually blocks the current user. Exact-name
ambiguity must be explicit; `.uasset` and `.umap` are identified as Unreal binary
assets. This tool is read-only and never unlocks or modifies a file.

## `list_workspaces`
List accessible clients/workspaces with owner, host, root, stream, and last access time.

## `get_workspace_details`
Return one workspace specification with sensitive fields removed.

## `search_depot_files`
Search depot paths by bounded pattern. Reject unbounded depot-wide searches unless explicitly allowed by policy.

## `get_file_metadata`
Return revision, type, size, digest, action, and head change for one or more depot files.

## `get_file_history`
Return bounded revision history for a depot file.

## `get_file_diff`
Return a bounded text diff or metadata-only result for binary Unreal assets.

## `get_file_locks`
Show whether files are locked, opened elsewhere, or exclusive-edit.

## `get_user_opened_files`
List files opened by a specific visible Perforce user, subject to policy.

## `get_changelist_details`
Return description, files, jobs, owner, status, and timestamps for a changelist.

## `get_submitted_changelists`
List recent submitted changelists using bounded filters.

## `search_changelists`
Search descriptions, users, paths, and date ranges with strict limits.

## `list_streams`
List streams visible to the current user.

## `get_stream_details`
Return a redacted stream specification.

## `compare_streams`
Return metadata and bounded file differences between streams.

## `get_server_health_summary`
Return only non-administrative availability and latency signals available to the user.

# Phase 3: Unreal and UGS reads

## `detect_unreal_projects`
Find configured Unreal projects within approved workspace roots.

## `get_unreal_project_status`
Summarise project file, engine association, modified assets, and source-control state.

## `get_unreal_asset_locks`
List `.uasset` and `.umap` lock/open status.

## `get_ugs_project_status`
Return detected UnrealGameSync configuration and selected project.

## `get_ugs_build_status`
Return latest build badges, changelist, result, and available log metadata.

## `get_ugs_sync_status`
Return current or last known sync state from the local companion.

## `get_build_log_excerpt`
Return a bounded, redacted excerpt for a selected failed build.

# Phase 4: safe write tools

All tools below require preview and confirmation support.

## `preview_sync_workspace`
Show files and estimated impact without syncing.

## `sync_workspace`
Sync to a validated changelist or approved path scope.

## `open_files_for_edit`
Open validated files for edit in a selected changelist.

## `open_files_for_add`
Open new files for add after path and workspace validation.

## `open_files_for_delete`
Open files for delete with enhanced confirmation.

## `create_pending_changelist`
Create a numbered changelist with a validated description.

## `update_changelist_description`
Change only the description of a pending changelist owned by the current user.

## `move_opened_files`
Move opened files between the current user's pending changelists.

## `shelve_changelist`
Shelve a pending changelist with preview.

## `unshelve_changelist`
Unshelve into a validated destination changelist and preview conflicts.

## `preview_resolve`
Report anticipated resolves without applying them.

## `resolve_files`
Apply only a selected resolve strategy to an explicit file set.

## `submit_changelist`
Submit one owned pending changelist after mandatory confirmation and final revalidation.

## `revert_files`
Revert explicit files with enhanced warning about local changes.

## `unlock_files`
Request or perform unlock only when the user has authority; never silently force unlock.

# Phase 5: local companion operations

## `list_connected_devices`
List paired devices and online state without revealing device secrets.

## `get_device_capabilities`
Return installed P4V, `p4`, UGS, and Unreal capabilities.

## `launch_p4v`
Open P4V locally for the active connection.

## `launch_unreal_game_sync`
Open UGS locally for a selected approved project.

## `launch_unreal_editor`
Launch a validated `.uproject` using the approved engine installation.

## `cancel_local_operation`
Cancel an operation that explicitly supports cancellation.

# Internal-only capabilities

The following must never be exposed as public MCP tools:

- Raw command execution
- Raw SQL or datastore access
- Token or ticket retrieval
- Arbitrary filesystem read/write
- Arbitrary process launch
- Perforce superuser administration
- Host-wide environment inspection

## Tool annotation policy

Each implemented tool must declare:

- Read-only versus write
- Whether confirmation is required
- Whether it can return sensitive repository data
- Expected maximum duration
- Idempotency characteristics
- Required local capability and Perforce permission
