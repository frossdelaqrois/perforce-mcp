# Hosted Gateway and Device Pairing

## Gateway responsibilities
- Authenticate ChatGPT app users
- Register, list, and revoke devices
- Route a request only to the selected authorised device
- Validate schemas, capability versions, permissions, and rate limits
- Store minimal audit metadata
- Return operation status to the MCP layer

The gateway must not provide arbitrary relay access and should not store Perforce passwords or tickets.

## Pairing flow
1. User signs in through the ChatGPT app or companion browser flow.
2. Companion creates a device key pair locally.
3. Gateway issues a short-lived single-use pairing challenge.
4. User confirms the matching device name/code.
5. Companion proves possession of its private key.
6. Gateway binds device public key to the user and approved organisation.
7. Companion receives a revocable device credential.
8. User approves one or more local workspaces separately.

## Request flow
1. ChatGPT invokes a tool.
2. Gateway authenticates user and resolves device/workspace.
3. Gateway creates a signed request with request ID, nonce, timestamp, tool, version, and bounded inputs.
4. Companion validates signature, expiry, replay state, capability, policy, and scope.
5. Companion executes or requests confirmation.
6. Companion signs the result.
7. Gateway verifies and returns a structured MCP response.

## Security requirements
- TLS for every network connection
- Device private keys never leave the device
- Short-lived user sessions
- Rotatable and revocable device credentials
- Replay prevention
- Strict user/tenant/device isolation
- Request and result size limits
- No sensitive data in queue names, URLs, or metrics labels
- Audit correlation using opaque request IDs

## Offline behaviour
- Read requests fail clearly when the selected device is offline.
- Requests are not queued indefinitely.
- Write requests expire quickly and require fresh confirmation after reconnect.
- The app may show cached connection metadata, clearly marked with its timestamp.

## Versioning
Every request includes protocol, capability, and schema versions. Unsupported combinations fail with upgrade guidance rather than silently degrading permissions.

## Data storage
Store only what is required for accounts, devices, permissions, routing, security logs, and operations. Source contents and diffs should be transient unless a user or organisation explicitly enables retention.