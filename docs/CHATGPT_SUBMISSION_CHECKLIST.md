# ChatGPT Submission Checklist

Revalidate every item against the current official OpenAI Apps SDK and app-submission documentation before submission.

## Product
- [ ] Clear user problem and audience
- [ ] Production HTTPS MCP endpoint
- [ ] Signed local companion installer and update flow
- [ ] New-user onboarding without manual MCP configuration
- [ ] Useful read-only experience
- [ ] Public name and trademark review

## Tools
- [ ] Precise tool names and descriptions
- [ ] Strict, bounded schemas
- [ ] Structured, size-limited outputs
- [ ] Accurate read/write annotations
- [ ] Confirmation for consequential actions
- [ ] No generic shell, filesystem, or raw P4 command tool
- [ ] Tool-selection and misuse evaluations pass

## UX
- [ ] Clear connection, authentication, and offline states
- [ ] Desktop and mobile layouts tested
- [ ] Empty, timeout, expired-login, and permission states designed
- [ ] Progress and final status for long operations
- [ ] Confirmations show exact scope and impact
- [ ] Accessibility review complete
- [ ] Conversation starters tested

## Security
- [ ] Encrypted, revocable device pairing
- [ ] User/device/tenant isolation tested
- [ ] Perforce credentials remain local where possible
- [ ] Replay protection, rate limits, and abuse controls
- [ ] Threat model and dependency scans complete
- [ ] Penetration test before public write-enabled release
- [ ] Incident response and credential-revocation procedures

## Privacy and legal
- [ ] Privacy policy at a stable public URL
- [ ] Terms of service
- [ ] Support contact and support page
- [ ] Data collection, retention, and deletion documented
- [ ] Account deletion and device revocation available
- [ ] Logs exclude secrets and source contents
- [ ] File contents and diffs are opt-in and bounded
- [ ] Subprocessors and hosting regions documented

## Reliability
- [ ] Monitoring, alerts, and status page
- [ ] Backup and disaster-recovery tests
- [ ] Offline-device and reconnection handling
- [ ] Timeout, cancellation, idempotency, and partial-failure tests
- [ ] Version compatibility and rollback policies
- [ ] Companion security-update mechanism tested

## Testing
- [ ] Unit and integration tests pass
- [ ] Disposable Perforce test environment used
- [ ] Windows installation tests pass
- [ ] P4V-only, CLI-only, and UGS setups tested
- [ ] Unreal asset lock workflows tested
- [ ] Large result and truncation tests pass
- [ ] Prompt-injection and permission-boundary tests pass
- [ ] Destructive-operation confirmation tests pass

## Listing assets
- [ ] Name, descriptions, category, and conversation starters
- [ ] Icon and screenshots in required formats
- [ ] Privacy, terms, support, and status URLs
- [ ] Reviewer instructions and test account if requested
- [ ] Local companion requirement explained clearly

## Submission
- [ ] OpenAI developer verification complete
- [ ] Submission information is accurate
- [ ] Reviewer can install, pair, and test with sample data
- [ ] Reviewer notes distinguish read-only and write capabilities
- [ ] Feedback tracked in GitHub
- [ ] Apps SDK changelog reviewed before every release
