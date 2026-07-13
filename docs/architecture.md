# Architecture

MeshCommander Enhanced uses one web application across all install targets.

## Components

- `tools/build-web.mjs` compiles the legacy MeshCommander feature-marked HTML into browser-safe static assets.
- `src/MeshCommander.Server` hosts the assets with ASP.NET Core and provides the websocket relay expected by the inherited AMT JavaScript.
- `src/MeshCommander.Windows` is a WinUI 3 WebView2 desktop shell.
- `src/MeshCommander.Desktop` is a Photino shell for macOS and Linux.
- `Dockerfile` publishes the ASP.NET Core server as a non-root container.

## Compatibility Choice

The upstream project contains mature AMT client code for WS-Man, KVM, SOL, and IDER. Rewriting all protocol layers before shipping a replacement would create high regression risk. This fork therefore modernizes the host, deployment, and shell first while keeping the AMT browser code intact.

## Relay

The old `webrelay.ashx` behavior is replaced by `WebSocketTcpRelay`. It keeps the same route for client compatibility, but adds:

- allowed AMT port checks
- target allow-list policy
- shared-interface authentication guard
- bounded buffers and cancellation
- optional strict TLS validation

## Future Rewrite Path

A later phase can move the front end to React or another typed UI and gradually replace inherited protocol modules behind integration tests. The current layout keeps that path open without delaying a usable release.
