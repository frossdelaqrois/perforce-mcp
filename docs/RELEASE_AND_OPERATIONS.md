# Release and Operations Plan

## Environments

- Local development
- Automated test environment with disposable Perforce server
- Internal alpha
- Limited external beta
- Production

Each environment must use separate credentials, databases, signing identities, and device-pairing keys.

## Release channels

### Companion
- Stable
- Beta
- Internal

The companion must support signed updates, staged rollout, rollback, and a minimum-supported-version policy.

### Hosted gateway

Use progressive deployment with health checks, backward-compatible schema changes, and rapid rollback.

## Versioning

Use semantic versioning for public components. Track protocol compatibility separately when necessary.

A release record should state:

- Gateway version
- Companion version
- MCP tool/schema version
- Supported P4 client/server ranges
- Supported Windows and UGS versions
- Breaking changes and migration instructions

## Monitoring

Monitor:

- Gateway availability and latency
- Authentication and pairing failures
- Online paired devices
- Tool success/error/timeout rates
- Queue and operation duration
- Companion crash/update failure rates
- Security alerts and rate-limit events

Do not include repository contents or secrets in metrics.

## Alerts

Define alert severity and response targets for:

- Complete outage
- Cross-tenant routing risk
- Authentication failure spike
- Companion update/signing problem
- Data leakage or secret exposure
- Elevated write-operation failure
- Dependency vulnerability

## Incident response

1. Detect and classify.
2. Contain, including disabling tools or revoking credentials.
3. Preserve minimal necessary evidence.
4. Communicate internally and to affected users as required.
5. Remediate and restore safely.
6. Complete a blameless review and track actions.

Maintain emergency controls to:

- Disable all write tools
- Disable one tool globally
- Revoke device certificates/tokens
- Require reauthentication
- Block a compromised companion version

## Backup and recovery

Back up only hosted configuration, account, policy, and audit data that is required. Perforce source data remains in users' Perforce systems.

Test restoration and document recovery objectives after the hosting design is selected.

## Rollback

- Gateway releases must be reversible without losing pairing state.
- Database changes should use expand/migrate/contract patterns.
- Companion releases need a signed previous-version rollback path where safe.
- Tool schemas should remain backward compatible during staged companion rollout.

## Status and support

Provide:

- Public status page
- Support contact and ticket workflow
- Known-issues page
- Security contact and vulnerability-reporting process
- Compatibility matrix

## Release gates

### Local prototype
Build, tests, read-only tools, and secret-redaction checks pass.

### Internal alpha
Signed companion, device pairing, basic telemetry, and rollback tested.

### External beta
Tenant isolation, privacy controls, support process, and production monitoring complete.

### Public ChatGPT submission
Submission checklist complete, security review passed, listing assets final, and reviewer environment available.
