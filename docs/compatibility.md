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

## Not Yet Done

- Full protocol rewrite into TypeScript
- Signed Windows installer
- macOS notarized app bundle
- Hardware validation matrix across AMT generations
- Automatic migration from old MeshCommander desktop storage
