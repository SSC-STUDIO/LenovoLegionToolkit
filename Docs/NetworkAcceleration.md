# Network Acceleration (built-in)

Independent UDT implementation inspired by Watt Toolkit *behavior* only.
**No GPL source** from Watt Toolkit / SteamTools is copied into this repository.

## Architecture

```
WPF (Network & acceleration page)
  └─ INetworkAccelerationService / INetworkDiagnosticsService / INetworkStateRecoveryService
        └─ UniversalDeviceToolkit.NetworkProxy.exe (isolated worker)
              ├─ Named pipe IPC (current-user ACL + random session token)
              └─ Loopback-only HTTP + CONNECT proxy (127.0.0.1 / ::1)
```

- **Default**: acceleration **OFF**. App launch never auto-starts proxy, Hosts edits, or certificates.
- **Crash isolation**: the proxy runs as a separate worker; failures must not tear down the GUI.
- **IPC**: named pipe, random session token per run, ACL limited to the current user (+ Administrators).
- **Bind**: loopback only — never `0.0.0.0` / `::`.
- **Startup recovery**: if a previous session left UDT system proxy / Hosts / orphaned workers, they are restored/killed without replaying acceleration.
- **Shutdown**: main app stops the worker and restores snapshot before exit.

## Modes

| Mode | Intent |
|---|---|
| `Off` | Default. No mutations. |
| `SystemProxy` | Point Windows system proxy / PAC at the local worker (user-started). |
| `Hosts` | Rewrite only the UDT-marked hosts block (`# BEGIN/END UDT-NETWORK-ACCELERATION`); may need elevation. |
| `DiagnosticsOnly` | Inspect / preview without changing system network state. |

## Domain groups

Built-in audited groups (disabled by default):

- **Steam** — steampowered.com, steamcommunity.com, steamstatic.com, …
- **GitHub** — github.com, githubusercontent.com, ghcr.io, …
- **Custom** — user-defined list (empty by default)

No third-party accelerator SDKs, no remote script injection, no unreviewed online rules store.

## Recovery

- Snapshot file: `%AppData%/.../network_state_snapshot.json` (via `Folders.AppData`).
- Captures: system proxy fields, UDT hosts block, PAC path/contents metadata.
- `--reset-network-state` clears `args.txt` proxy passthrough, stops the worker, and restores from snapshot.
- UI: **Force restore network state**.
- Missing / empty snapshot is an **idempotent success** (safe to run repeatedly).
- Partial failures are reported item-by-item; other steps still run.

## HTTPS / local CA (planned / optional)

- Selective HTTPS decryption requires explicit user consent.
- Local CA is generated in **CurrentUser** store only; private key protected with DPAPI.
- Not written to computer-level root store by default.
- Current worker ships CONNECT tunneling without MITM by default.

## Status

| Piece | Status |
|---|---|
| `UniversalDeviceToolkit.NetworkProxy` worker + IPC | Done (HTTP + CONNECT, loopback) |
| Lib interfaces + config + hosts/PAC helpers | Done |
| Snapshot restore + startup heal + shutdown stop | Done |
| System proxy / PAC apply on user Start | Done |
| Hosts mode (marked block, best-effort elevation) | Done |
| WPF page (enable, start/stop, restore, diagnostics) | Done |
| Built-in Steam/GitHub domain groups (off by default) | Done |
| YARP MITM / DPAPI CA UI | Optional follow-up |
| Continuous background sampling when page hidden | **Not used** (by design) |

## Plugin consolidation

Official plugins live in [UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins).
See `Docs/PluginConsolidation.md`.

- **Network Acceleration** plugin: legacy / migration-only, store Offline.
- **Battery Health** plugin: legacy; thresholds merge into main battery/sensors.
- **Custom Mouse** → **Cursor & Pointer**.
- **Shell Integration** → **Nilesoft Shell Manager**.
- **ViVeTool**: keep with risk copy.
