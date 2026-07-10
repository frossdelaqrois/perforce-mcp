# UnrealGameSync and P4V Integration

## P4V integration

P4V is optional for core functionality; the command-line client remains the source of truth for automation.

Planned capabilities:
- Detect installed P4V versions
- Launch P4V with the active connection/workspace
- Open a selected depot path or changelist where supported
- Surface P4V-required workflows without scraping its UI

Do not automate P4V by simulated clicks or unrestricted UI scripting.

## UnrealGameSync integration

Planned read capabilities:
- Detect UGS installation
- Detect configured project and workspace
- Read approved project configuration
- Report latest build badge/status
- Report selected changelist and sync state
- Return bounded build-log excerpts

Planned later actions:
- Launch UGS for an approved project
- Trigger an approved sync/build flow through the local companion
- Cancel operations where UGS safely supports it

## Unreal-specific rules

- Treat `.uasset` and `.umap` as binary unless a trusted parser is explicitly added.
- Do not claim to understand binary asset changes from a text diff.
- Highlight exclusive-lock and opened-elsewhere states.
- Keep Unreal project discovery within approved workspace roots.
- Redact usernames and local paths according to organisation policy.

## Build-log analysis

- Extract the first actionable error before cascading failures.
- Preserve the exact source line in a bounded details section.
- Clearly separate observed errors from inferred causes.
- Apply prompt-injection and secret redaction protections.
