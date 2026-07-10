# Release and Operations Plan

## Environments
- Local development
- Disposable integration test
- Staging with production-equivalent authentication and pairing
- Production

Never test write operations against a valuable production depot.

## Release channels
- Internal
- Alpha read-only
- Beta read-only
- Beta write-enabled
- Public

## CI gates
- Restore/build/test
- Formatting and static analysis
- Dependency and secret scanning
- Tool-schema compatibility checks
- Evaluation suite
- Installer verification when companion exists

## Versioning
Use semantic versioning for public components. Version MCP schemas and device capabilities independently when necessary. Breaking tool changes require migration notes and compatibility handling.

## Deployment
- Immutable gateway releases
- Staged rollout
- Health checks before traffic shift
- Automatic rollback for clear reliability regressions
- Manual approval for permission or data-handling changes

## Monitoring
Track:
- Availability and latency
- Authentication and pairing failures
- Device online rate
- Tool success/error rates by stable code
- Timeouts and cancellations
- Confirmation abandonment
- Companion version distribution

Never put depot paths, filenames, usernames, changelist descriptions, or source content in metric labels.

## Incident response
1. Triage severity and affected scope.
2. Revoke compromised credentials/devices where needed.
3. Disable affected tools using a server-side kill switch.
4. Preserve minimal security evidence.
5. Communicate status and recovery steps.
6. Publish a post-incident review for material incidents.

## Rollback
- Gateway rollback to last compatible build
- Companion update rollback where safe
- Tool-level feature flags
- Read-only emergency mode
- Disable hosted routing without affecting local data

## Backups
Back up account, device, policy, and audit metadata according to retention policy. Do not back up transient source content unless explicitly required and disclosed.

## Support
Provide a support page, diagnostic guide, status page, security contact, and documented response targets before public submission.
