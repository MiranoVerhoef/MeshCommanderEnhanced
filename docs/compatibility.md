# Compatibility

## Preserved

- Intel AMT WS-Man management over ports 16992 and 16993
- Redirection sessions over ports 16994 and 16995
- Remote desktop/KVM client path
- Serial-over-LAN terminal path
- IDER client path
- Local computer list workflow from MeshCommander
- Localized HTML entry points from upstream

## Changed

- NodeWebkit desktop mode is replaced by native shells.
- ASP.NET `webrelay.ashx` is replaced by ASP.NET Core.
- The UI receives an additional modern stylesheet while keeping upstream DOM and logic.
- Docker deployments require an admin token when bound to a shared interface.
- Installed desktop clients use a stable loopback origin so their browser storage survives restarts.

## Legacy migration

On first launch, the desktop sidecar searches the standard legacy profile location for the current platform:

- Windows: `%LOCALAPPDATA%\MeshCommander`
- macOS: `~/Library/Application Support/MeshCommander`
- Linux: `~/.config/MeshCommander`

The importer reads the legacy Chromium `.localstorage` database, decodes the Node/OpenSSL AES-CTR format used for saved computers, and imports supported machine and preference keys. Existing MeshCommander Enhanced values win, so upgrading or rerunning migration never overwrites newer configuration. Private certificate-store material is intentionally excluded.

## Not Yet Done

- Full protocol rewrite into TypeScript
- Authenticode-signed Windows installer
- macOS notarized app bundle
- Hardware validation matrix across AMT generations
