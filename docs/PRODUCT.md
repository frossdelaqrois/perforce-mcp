# Product Definition

## Vision

A Perforce app discoverable in the ChatGPT app directory, giving developers a safe and understandable way to work with Helix Core, P4V, and UnrealGameSync.

## First audience

Small and medium Unreal Engine teams using Windows workstations and a Perforce server.

## First release value

Users can connect a device and ask ChatGPT to inspect workspace status, opened files, pending changelists, file history, locks, and UnrealGameSync build status.

## Out of scope for the first release

- General remote desktop control
- Arbitrary shell commands
- Perforce server administration
- Silent destructive operations
- Full replacement for P4V

## Success measures

- A new user can connect without understanding MCP configuration.
- Read-only answers match P4V and `p4` output.
- Write actions, when added, are previewed and confirmed.
- No Perforce credentials are exposed to ChatGPT.
- The app meets the current ChatGPT app-directory requirements.