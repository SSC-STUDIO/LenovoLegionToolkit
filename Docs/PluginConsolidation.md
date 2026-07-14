# Plugin Consolidation Matrix

Source of truth for store metadata remains the sibling repo
`UniversalDeviceToolkit-Plugins/store.json` (fetched via CDN / GitHub).
This main repo keeps planning stubs under `Packaging/plugins/`.

| Plugin id | Store status (sibling) | UDT plan | Notes |
|---|---|---|---|
| `network-acceleration` | **Removed** (v5.0.0) | **Built-in** Network & acceleration | Plugin source deleted; host auto-uninstalls local copies on startup. |
| `battery-health` | **Offline** / Legacy | Merge thresholds into main Battery / Sensors | Plugin retained for settings migration until removalVersion. |
| `custom-mouse` | **Active** as **Cursor & Pointer** | Rename done; remove fake DPI/polling UI | Pointer speed, button swap, light/dark cursor, theme follow, backup/restore kept. |
| `shell-integration` | **Active** as **Nilesoft Shell Manager** | Keep name | Requires Nilesoft Shell; show install guidance when missing. |
| `vive-tool` | Active | Keep; strengthen risk copy | Feature-flag changes can destabilize Windows. |

## Network Acceleration migration

1. Stop relying on the plugin for runtime acceleration.
2. Use built-in **Network & acceleration** page (default off).
3. Recovery: `--reset-network-state` + snapshot restore + **Force restore network state**.
4. Plugin removed from repo and store; host prunes installed copies on startup.
5. Do **not** migrate Gaming/Streaming mode names as real acceleration strategies.
6. Destructive auto operations (Winsock/TCP-IP reset on startup) stay disabled.

## Battery Health migration

1. Prefer main program battery / sensor data (no duplicate WMI polling).
2. Thresholds and real background notifications merge into main battery features.
3. Store Offline after migration messaging.

## Custom Mouse → Cursor & Pointer

1. Rename in store / plugin.json / resources.
2. Keep: pointer speed, L/R swap, light/dark cursors, follow theme, backup/restore.
3. Remove: DPI / polling-rate UI that only saved numbers without applying hardware changes.

## Shell Integration → Nilesoft Shell Manager

1. Name and copy must state the Nilesoft Shell dependency.
2. When Nilesoft is not installed, show install guidance only — never claim generic Windows shell integration.

## ViVeTool

1. Remain optional advanced plugin.
2. Emphasize risk, status backup, and restore instructions.
