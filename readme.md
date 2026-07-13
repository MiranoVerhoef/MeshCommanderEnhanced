# MeshCommander Enhanced

MeshCommander Enhanced is a modernized continuation of Ylian Saint-Hilaire's MeshCommander codebase for Intel AMT management.

This fork keeps the proven browser AMT, WS-Man, terminal, KVM, SOL, and IDER client code, then wraps it with:

- ASP.NET Core 8 static hosting and websocket-to-AMT relay
- WinUI 3 desktop shell for Windows
- Photino-based desktop shell for macOS and Linux
- Docker and Compose deployment
- GitHub Actions CI and release artifacts
- Modernized compatibility UI styling

The intent is compatibility first: keep the original AMT behavior working while replacing the old NodeWebkit / ASP.NET `webrelay.ashx` hosting model with maintained tooling.

## Quick Start

### Local Web Server

```powershell
npm run build:web
dotnet run --project src/MeshCommander.Server
```

Open `http://127.0.0.1:3000`.

By default, the relay only allows private, loopback, and unique-local AMT targets. Set `MCE_ALLOWED_TARGETS` to a comma-separated allow list if your management network is different.

### Docker

Set a real token before exposing the service:

```powershell
$env:MCE_ADMIN_TOKEN = "replace-with-a-long-random-token"
docker compose up --build
```

Then open `http://127.0.0.1:3000` and sign in with:

- user: `meshcommander`
- password: your `MCE_ADMIN_TOKEN`

### Desktop

Installers are published on the GitHub Releases page:

- Windows x64: Inno Setup `.exe` (per-user install, Start Menu shortcut, clean upgrades/uninstall)
- macOS Apple Silicon: native `.app` inside a drag-to-Applications `.dmg`
- Linux x64: `.deb` package with application-menu integration

Portable ZIP packages remain available for troubleshooting and managed deployment.

On first desktop launch, MeshCommander Enhanced looks for the legacy MeshCommander Chromium profile and imports the saved computer list and compatible preferences. Existing Enhanced data is never overwritten. Desktop clients use the stable local origin `http://127.0.0.1:16990`, so settings persist across launches and upgrades.

Unsigned community builds can still show Windows SmartScreen or macOS Gatekeeper warnings. Authenticode signing and Apple notarization require project signing credentials that are not stored in this repository.

For development, the cross-platform shell can be built with:

Cross-platform shell:

```powershell
dotnet build src/MeshCommander.Desktop/MeshCommander.Desktop.csproj
```

Windows WinUI 3 shell:

```powershell
dotnet build src/MeshCommander.Windows/MeshCommander.Windows.csproj -c Release
```

The WinUI 3 build requires Visual Studio 2022 or Build Tools with Windows App SDK / Windows application packaging tasks installed. The plain .NET SDK alone is not enough.

## Configuration

| Variable | Default | Purpose |
| --- | --- | --- |
| `ASPNETCORE_URLS` | `http://127.0.0.1:3000` | HTTP bind address. Binding to `0.0.0.0` requires `MCE_ADMIN_TOKEN`. |
| `MCE_ADMIN_TOKEN` | empty | Enables Basic auth for shared or container deployments. |
| `MCE_ALLOWED_TARGETS` | `private` | Relay target allow list: `private`, `*`, exact host/IP, wildcard domains, or IPv4 CIDR. |
| `MCE_ALLOW_UNTRUSTED_AMT_TLS` | `true` | Allows self-signed AMT TLS certificates. Set to `false` for strict certificate validation. |
| `MCE_DESKTOP_URL` | `http://127.0.0.1:16990` | Stable loopback origin used by installed desktop clients. |

## Build And Test

```powershell
npm run test:web
npm run build:web
dotnet test src/MeshCommander.Server.Tests/MeshCommander.Server.Tests.csproj
docker build -t meshcommander-enhanced:local .
```

## Status

This is a compatibility-first modernization. The inherited AMT browser modules are still the upstream MeshCommander implementation. The host, relay, deployment model, shell projects, and styling are new. Hardware-level validation against multiple Intel AMT firmware generations is still required before treating this as a production replacement.

## License

MeshCommander Enhanced remains under the Apache License 2.0. Original MeshCommander source is copyright Ylian Saint-Hilaire and contributors.
