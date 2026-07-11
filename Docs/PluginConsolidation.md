# Plugin Consolidation Matrix

Source of truth for store metadata remains the sibling repo
`UniversalDeviceToolkit-Plugins/store.json` (fetched via CDN / GitHub).
This main repo keeps planning stubs under `Packaging/plugins/`.

| Plugin id | Store status (sibling) | UDT plan | Notes |
|---|---|---|---|
| `network-acceleration` | Legacy / migration-only | **Built-in** Phase 1 foundation | Disable auto optimize + continuous sampling on plugin startup; migrate settings into `network_acceleration.json`. |
| `battery-health` | Legacy | Merge thresholds into main Battery / Sensors | Plugin source is sibling-only; Phase 1 documents matrix — full merge later. |
| `custom-mouse` | Active (rename pending) | Rename to **Cursor & Pointer**; remove fake DPI/polling UI | Rename + UI cleanup tracked in plugins repo. |
| `shell-integration` | Active as **Nilesoft Shell Manager** | Keep name | Already renamed in store.json. |
| `vive-tool` | Active | Keep; strengthen risk copy | Feature-flag changes can destabilize Windows. |

## Network Acceleration migration

1. Stop relying on the plugin for runtime acceleration.
2. Use built-in **Network & acceleration** page (default off).
3. Recovery: `--reset-network-state` + snapshot restore.
4. Plugin retained only to migrate existing configuration.
