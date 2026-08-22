# Linux demo Host stub

Electron is UI-only. On Linux there is no `UniversalDeviceToolkit.Host` with LibreHardwareMonitor, WMI, or Legion EC, so recordings previously spawned a **minimal** JSON-RPC stub that answered just enough for the shell to load. That stub returned empty sensor snapshots (`initialized: false`) and `feature.list` with every key `supported: false`, so gauges stayed on skeleton / 等待传感器数据 and dashboard cards vanished.

`host.mjs` is a complete-enough Host stand-in for promo / Linux UI work:

| Domain | Behaviour |
|---|---|
| `system.info` | `model: Linux Desktop` (never a Legion SKU) |
| `sensors.getSnapshot` + `sensors.updated` | CPU/GPU/battery numbers in the renderer `SensorSnapshot` shape |
| OS features (`microphone`, `speaker`, `hdr`, `resolution`, `refreshRate`, `dpiScale`) | supported, with sample states |
| Legion EC / RGB / Vantage / Hotkeys / boot logo | `supported: false` or JSON-RPC `-1001` with the Windows Host reason |
| Optimization / automation / macros / plugins / settings | sample or empty-but-supported lists |

## Launch with v6.0.0 Electron

From a worktree at tag `v6.0.0` plus `cursor/linux-opaque-backdrop-6fe9`:

```bash
chmod +x Docs/promo/linux-host-stub/UniversalDeviceToolkit.Host
DISPLAY=:1 ELECTRON_DISABLE_SANDBOX=1 \
  UDT_HOST_PATH=/absolute/path/to/Docs/promo/linux-host-stub/UniversalDeviceToolkit.Host \
  npx electron-vite dev -- --no-sandbox --disable-dev-shm-usage --ozone-platform=x11
```

Or copy this folder to `/tmp/udt-stub-host` (the path used in `Docs/promo/README.md`).

```bash
node Docs/promo/linux-host-stub/smoke.mjs
```
