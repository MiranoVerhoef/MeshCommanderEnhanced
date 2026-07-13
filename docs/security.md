# Security Notes

MeshCommander Enhanced can open powerful out-of-band management sessions. Treat the web server and Docker deployment as administrative tools.

## Defaults

- Local server binds to `127.0.0.1:3000`.
- Binding to `0.0.0.0`, `*`, or `+` requires `MCE_ADMIN_TOKEN`.
- Relay targets default to `MCE_ALLOWED_TARGETS=private`.
- Link-local metadata ranges such as `169.254.169.254` are blocked.

## Docker

The Compose file runs with:

- non-root container user
- read-only filesystem
- dropped Linux capabilities
- `no-new-privileges`
- Basic auth via `MCE_ADMIN_TOKEN`

Change the sample token before use.

## TLS

Intel AMT devices often ship with self-signed certificates. For compatibility, `MCE_ALLOW_UNTRUSTED_AMT_TLS` defaults to `true`. Set it to `false` in managed certificate environments.

## Known Inherited Risks

The browser protocol modules are inherited from upstream MeshCommander. They still handle credentials, certificates, and AMT protocol parsing in legacy JavaScript. Do not expose the UI to untrusted users.
