# Network Acceleration (Phase 1)

Independent UDT implementation inspired by Watt Toolkit *behavior* only.
**No GPL source** from Watt Toolkit / SteamTools is copied into this repository.

## Architecture

```
WPF (Network & acceleration page)
  └─ INetworkAccelerationService / INetworkDiagnosticsService / INetworkStateRecoveryService
        └─ (later) spawn UniversalDeviceToolkit.NetworkProxy.exe
              ├─ Named pipe IPC (current-user ACL + random session token)
              └─ Loopback-only listener (127.0.0.1 / ::1)
```

- **Default**: acceleration **OFF**. App launch never auto-starts proxy, Hosts edits, or certificates.
- **Crash isolation**: the proxy runs as a separate worker; failures must not tear down the GUI.
- **IPC**: named pipe, random session token per run, ACL limited to the current user (+ Administrators).
- **Bind**: loopback only — never `0.0.0.0` / `::`.

## Modes

| Mode | Intent |
|---|---|
| `Off` | Default. No mutations. |
| `SystemProxy` | Point Windows system proxy at the local worker (Phase 2+). |
| `Hosts` | Rewrite only the UDT-marked hosts block (`# BEGIN/END UDT-NETWORK-ACCELERATION`). |
| `DiagnosticsOnly` | Inspect / preview without changing system network state. |

## Recovery

- Snapshot file: `%AppData%/.../network_state_snapshot.json` (via `Folders.AppData`).
- Captures: system proxy fields, UDT hosts block, PAC path/contents metadata.
- `--reset-network-state` (existing startup flag) clears `args.txt` proxy passthrough **and** restores from snapshot.
- Missing / empty snapshot is an **idempotent success** (safe to run repeatedly).

## Phase 1 status

| Piece | Status |
|---|---|
| `UniversalDeviceToolkit.NetworkProxy` worker skeleton + IPC | Done (stub listener) |
| Lib interfaces + config + hosts/PAC helpers | Done |
| Snapshot restore | Done (apply/start still stubbed) |
| WPF page + nav | Done (Start/Stop disabled until backend ready) |
| WPF auto-start of worker | **Not wired** (intentional) |
| YARP CONNECT / HTTPS MITM / DPAPI CA | Out of scope |
| Production Steam/GitHub rules packs | Out of scope |

## Plugin consolidation notes

Official plugins live in [UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins).
Store JSON: remote `store.json` (not under this repo's `Packaging/` by default). Local stubs: `Packaging/plugins/`.

See `Docs/UpstreamCapabilityMatrix.md` and `Docs/PluginConsolidation.md`.
