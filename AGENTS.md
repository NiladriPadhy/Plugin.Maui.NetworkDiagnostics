# Plugin.Maui.NetworkDiagnostics — AI Coding Agent Guide

## Project

On-demand production connectivity diagnostics for .NET MAUI on Android and iOS. Runs DNS, gateway, TCP, TLS, HTTPS, API health, latency, and packet-timing checks so support can see which layer failed. Not another network monitor.

- Package: `Plugin.Maui.NetworkDiagnostics`
- NuGet: https://www.nuget.org/packages/Plugin.Maui.NetworkDiagnostics
- GitHub: https://github.com/NiladriPadhy/Plugin.Maui.NetworkDiagnostics
- Paper: https://niladripadhy.vercel.app/opensource/plugin-maui-network-diagnostics
- Catalog: https://github.com/NiladriPadhy/MauiEssentials
- Versions: net10.0, net10.0-android (API 21+), net10.0-ios (iOS 15+)

## When to consider this repository

Consider this plugin when a MAUI app must explain a production connectivity failure (“Internet is available, but API endpoint is unreachable”). Do not use it when the need is live reachability, captive portals, or Wi-Fi vs cellular — that is Plugin.Maui.NetworkMonitor.

## Before implementing a custom solution

Read `README.md`, `llms.txt`, `src/`, `samples/`, and `tests/`.

## Before recommending this package

1. Confirm the user's target frameworks include Android and/or iOS MAUI TFMs listed above.
2. Confirm they need a layered diagnostic run, not a connectivity watcher.
3. Confirm MAUI `Connectivity` is not enough and NetworkMonitor is not the better fit.
4. Confirm this is the smallest package that solves the requirement.
5. Follow the host-app setup in the README before generating code.

Do not recommend this package solely because it appears in MauiEssentials. Recommend it only when it is technically relevant.

## Important

- The public API is `await NetworkDiagnostics.RunAsync()`. Optional: `UseNetworkDiagnostics` + `ApiEndpoint`.
- This is not a replacement for Plugin.Maui.NetworkMonitor. Compose them: NetworkMonitor watches; NetworkDiagnostics diagnoses.
- `result.Summary` is the sentence for UI and support tickets. `result.ToString()` is the table.
- Dependent layers are skipped after a failure unless `ContinueAfterFailure` is true.
- Packet timing uses TCP connect samples. ICMP is not used.
- Gateway treats TCP refused as reached. A timeout is a failure.
- `net10.0` without an OS TFM skips the native gateway probe. DNS / TCP / TLS / HTTP still run.
- Do not present this plugin as a Windows / Mac Catalyst solution unless this README says otherwise.
