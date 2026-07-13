# Universal Device Toolkit v5.0.0

**Release date:** 2026-07-14

## Downloads

| Asset | Description |
|-------|-------------|
| `UniversalDeviceToolkitSetup-Full.exe` | Full installer (recommended) — bundled languages & device support |
| `UniversalDeviceToolkitSetup-Online.exe` | Online installer — smaller base; extra resources from in-app catalog |
| Portable ZIPs under `release-assets/` | Full / Online portable win-x64 packages when produced by Make.bat |
| Cross-platform CLI (5.x) | Built when `Make.bat` enables cross-platform CLI for major ≥ 5 |

Verify SHA256 against the manifest generated next to the release assets.

---

## Highlights

### Loading chrome & skeleton 流光
- Dashboard owns loading chrome (no multi-stage nav shell flash) with a **detailed sensors skeleton** (title/model, gauge, metrics, trend well, legend).
- Plugin Extensions skeleton on cold and re-entry visits; stronger shimmer contrast; soft handoff without stuck blank frames.
- Shared `SkeletonShimmer` / loading infrastructure cleanup.

### Plugin Extensions opt-in
- **Plugin Extensions nav is off by default** (opt-in under Settings → Navigation items).
- Persistent status notice when hidden; close is session-only; fixed false “dismissed” on startup.

### Sensors & fan RPM
- Faster Lenovo fan WMI path with soft handling of `Invalid object` / 无效的对象 (no debugger spam, retry with fresh instance).
- Multi-source fan speed coordinator (WMI / Gamezone / capability / LHM fallbacks).

### Theme & UI polish
- **Official Cool** (and other style presets) retint cards, charts, and notification glass — not only control fills.
- Settings **CardAction** corner radius matches **CardControl** (`CornerRadiusCard`).
- Network acceleration status chip: short label + soft tint + detail under the chip.
- Notification toast width/glass language aligned across snackbar, host, and status banners.

### Reliability
- Navigation soft-fade no longer leaves LoadingChrome pages at Opacity 0.
- Device setup / startup orchestration and related stability fixes from the 5.0 train.

---

## Upgrade notes

1. **Plugin Extensions** may disappear from the sidebar after upgrade if it was only “default on” from older builds. Enable it in **Settings → Navigation items**.
2. First launch after upgrade runs a one-time settings migration (`PluginExtensionsOptInMigrationDone`) to apply the opt-in default.
3. Requires **.NET 10 Desktop Runtime (x64)** and Windows 10 1809+ / Windows 11.

---

## Build

```bat
REM From repo root, with Inno Setup 6 on PATH (or use full path to ISCC.exe)
Make.bat 5.0.0
```

Outputs:
- `Build\` / `Build-English\` — published payloads  
- `BuildInstaller\UniversalDeviceToolkitSetup-Full.exe`  
- `BuildInstaller\UniversalDeviceToolkitSetup-Online.exe`  
- `release-assets\` — packaged assets + hashes  

---

## Testing (pre-release checklist)

- [x] Release publish WPF + CLI + NetworkProxy (4.2.1 train → 5.0.0)
- [x] Unit tests (dashboard / plugin loading / skeleton / fan coordinator)
- [ ] Full installer install on clean machine
- [ ] Cold start → Dashboard sensors skeleton → live gauges
- [ ] Plugin Extensions hidden by default + status banner
- [ ] Settings → Navigation items → enable Plugin Extensions
- [ ] Official Cool theme: sensors card + toast glass tint
- [ ] Fan RPM non-zero or graceful `-` with LHM fallback
