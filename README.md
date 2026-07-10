# Perforce for ChatGPT

A planned ChatGPT app that lets developers safely inspect and work with Perforce repositories through natural language.

The long-term goal is publication in the ChatGPT app directory as a Perforce counterpart to the GitHub app. The MCP server in this repository is the technical foundation, not the final product.

## Intended users

- Unreal Engine teams using P4V and UnrealGameSync
- Game studios using Helix Core
- Developers using ChatGPT, Codex, or another MCP-compatible client

## Example requests

- "Show the files I have checked out."
- "Who has this Unreal map locked?"
- "Summarise my pending changelist."
- "Explain this Perforce error."
- "Show the latest UnrealGameSync build status."
- "Shelve these files for review." — later phase, with confirmation

## Planned architecture

```text
ChatGPT app
    |
    v
Hosted authenticated MCP gateway
    |
    v
Secure local Windows companion
    |
    +-- p4.exe / P4V
    +-- UnrealGameSync
    +-- Unreal Engine workspaces
```

A hosted service cannot directly run commands on a developer's PC. The local companion will execute narrowly defined operations and require confirmation for destructive or high-impact actions.

## Safety principles

- Read-only tools first
- No arbitrary shell command tool
- Explicit confirmation for submit, revert, unlock, force-sync, or delete operations
- Least-privilege Perforce credentials
- Clear audit logs
- Secrets stored using platform credential storage
- Structured tool responses with redaction of sensitive paths and tokens

## Delivery phases

1. Read-only local MCP prototype
2. Perforce command adapter and structured results
3. Secure Windows companion for P4V and UnrealGameSync
4. Hosted authenticated MCP gateway
5. ChatGPT Apps SDK interface and rich cards
6. Privacy, security, support, and reliability work
7. ChatGPT app-directory submission
8. Optional Codex and other MCP-client compatibility

See [ROADMAP.md](ROADMAP.md) and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Current milestone

Build a local read-only prototype exposing:

- `get_perforce_info`
- `get_opened_files`
- `get_pending_changelists`

## Project status

Early planning and foundation stage. APIs and architecture will change.

## Naming note

The repository currently has a leading hyphen in its GitHub name (`-perforce-mcp`). Renaming it to `perforce-mcp` is recommended before public release.

## License

A license has not yet been selected. Add an appropriate open-source license before accepting external contributions.