# Security Model

## Objective

Provide useful Perforce access to ChatGPT without turning the integration into remote shell access or exposing studio credentials and source code unnecessarily.

## Core rules

- Default to read-only access.
- Expose narrow, purpose-built tools.
- Keep Perforce credentials on the user's device where possible.
- Require explicit confirmation for consequential actions.
- Log actions, not secrets.
- Minimise data sent to hosted services and ChatGPT.

## Prohibited designs

- Arbitrary shell or PowerShell execution
- Generic raw Perforce command execution
- Passing user-controlled strings through a shell
- Storing Perforce passwords in source code or configuration committed to Git
- Returning Perforce tickets or full environment-variable dumps
- Silent submit, revert, delete, force-sync, or unlock operations

## Process execution requirements

- Invoke the validated `p4` executable directly.
- Pass each argument separately.
- Apply command-specific allowlists.
- Use timeouts and cancellation.
- Limit stdout and stderr sizes.
- Reject unexpected binary output.
- Normalise and redact errors before returning them.

## Data handling

Potentially sensitive data includes:

- Depot paths
- Local filesystem paths
- Usernames and server addresses
- Changelist descriptions
- Source-code diffs and file contents
- Perforce tickets and authentication errors

Only return data required for the requested task. File contents and diffs must be opt-in and size-limited in later phases.

## Error explanation boundary

`explain_perforce_error` treats every supplied message as untrusted data, not as
instructions. The initial classifier is deterministic and local: it uses bounded
rules and fixed guidance rather than an AI/model call. Invoking it does not run
`p4` or another process and does not read environment variables or local files.

Input error text is capped at 4,096 characters and is never echoed in the
response. The response contains only fixed summaries, observed classification
indicators, separately identified possible causes, and safe verification steps.
Credential-like values, embedded connection credentials, sensitive command
arguments, username-like values, and local paths produce small generic redaction
indicators; their original values are omitted. Instruction-like or fake MCP
content is ignored. Ambiguous matches remain explicitly ambiguous and do not
authorize or trigger corrective actions.

## Write actions

Every write action should have:

1. An operation-specific tool
2. Input validation
3. A preview or summary
4. Explicit user confirmation
5. A local authorisation check
6. An audit entry
7. A structured final result and recovery guidance

## Threats to test

- Command and argument injection
- Malicious depot filenames or changelist text
- Prompt injection embedded in repository content
- Cross-user or cross-device request routing
- Stolen pairing tokens
- Excessive output and denial of service
- Secret leakage through logs and exceptions
- Symlink or path traversal attacks when file access is added

This document is an initial model and must be expanded before hosted or write-enabled releases.
