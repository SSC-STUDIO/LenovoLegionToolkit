# UI Performance Testing

This document describes how to measure **every main shell surface** of Universal Device Toolkit and which analysis tools to use.

## Quick start

From an interactive Windows desktop session (elevated recommended if the app requires admin):

```powershell
cd C:\path\to\UniversalDeviceToolkit

# Full suite: build + backend benches + all UI surfaces
.\Scripts\Run-UiPerformanceSuite.ps1 -Configuration Release -Iterations 2
```

Outputs land under `_ui_perf_out/<timestamp>/`:

| File | Contents |
|------|----------|
| `ui/ui-perf-report.md` | Per-page ready/settle latency, memory delta, UIA complexity |
| `ui/ui-perf-report.json` | Machine-readable same data |
| `backend-performance.txt` | Logging / WMI / IO / settings microbenches |
| `SUITE-SUMMARY.md` | Combined summary + tool notes |

### UI-only re-run

```powershell
dotnet build Tools\UiPerformance.Smoke\UiPerformance.Smoke.csproj -c Release -p:Platform=x64
dotnet run --project Tools\UiPerformance.Smoke\UiPerformance.Smoke.csproj -c Release -p:Platform=x64 -- `
  --repo-root . --out _ui_perf_out\manual --configuration Release --iterations 2
```

### Surfaces covered

| Id | UI surface |
|----|------------|
| `dashboard` | Home / sensors control center |
| `keyboard` | Keyboard backlight |
| `automation` | Actions / automation |
| `macro` | Macro page |
| `winopt-home` | System optimization root |
| `winopt-optimization` | Optimization tab |
| `winopt-cleanup` | Cleanup tab |
| `winopt-driver` | Driver download tab |
| `winopt-network` | Network acceleration tab |
| `plugins` | Plugin extensions |
| `settings` | Settings |
| `about` | About |
| `device-info-dialog` | Device information dialog |

### Rating thresholds (ready latency)

| Rating | Median ready time | UDT Status |
|--------|-------------------|------------|
| excellent | ≤ 400 ms | **All pages meet this baseline** |
| good | ≤ 900 ms | - |
| fair | ≤ 1800 ms | - |
| needs work | > 1800 ms | - |

## Architectural Performance Pillars

UDT dispels the "Electron is bloated" stereotype through disciplined engineering:

1. **Zero-Memory Tray Sleeping**: Main window and renderer DOM tree are fully destroyed when minimized/closed to tray (`enterBackground()`). Memory returns to lean baseline instead of keeping hidden Chromium DOM structures.
2. **Hot-Path Zero Allocation**: High-frequency 1Hz sensor polling streams incremental diffs. Static ECharts configs and UI option maps are cached with `useMemo`, eliminating GC spikes in render cycles.
3. **Suspended Background Polling**: Sensor and network acceleration polling timers automatically stop whenever the UI surface is hidden.
4. **Graph-Based Code Splitting**: 7,000+ Fluent UI icons and page bundles are dynamically loaded per route without dragging large monolithic vendor bundles into the critical render path.

## Analysis tools (use together)

### 1. Visual Studio Performance Profiler

1. Open the Electron / Host solution projects you are investigating
2. **Debug → Performance Profiler** (Alt+F2) when profiling a .NET host process
3. Enable:
   - **CPU Usage** — find expensive page / sensor refresh paths
   - **.NET Object Allocation** — find allocation spikes on navigate
   - **UI Analysis** (when available) — layout/render stalls
4. Navigate every sidebar item while recording
5. Stop and inspect hottest stacks by page transition

### 2. `dotnet-counters` (live runtime)

```powershell
dotnet tool install -g dotnet-counters   # once

# While UDT Host is running:
dotnet-counters monitor --process-id <pid> `
  System.Runtime
```

Watch: `Time in GC`, `Allocation Rate`, `Working Set`.

### 3. `dotnet-trace` / PerfView (deep stacks)

```powershell
dotnet tool install -g dotnet-trace
dotnet-trace collect -p <pid> --duration 00:01:00 --providers Microsoft-Windows-DotNETRuntime
```

Or PerfView:

```text
PerfView /nogui collect /MaxCollectSec:60
# flip every page, then open the .etl.zip in PerfView → CPU Stacks / GC Stats
```

### 4. Windows-hosted Linux verification through WSL

When a Windows developer needs to validate the real Linux TFM and host probes,
run this from the repository root after installing a WSL distribution with the
.NET 10 SDK:

```powershell
.\Scripts\Test-CrossPlatformInWsl.ps1 -Distro Ubuntu -Configuration Release
```

The script runs locked restore, builds the Linux platform assembly and portable
test project, runs the cross-platform tests, and smoke-tests the diagnostics CLI
inside WSL. It fails when WSL or the requested distribution is unavailable.

## Interpreting results

| Symptom | Likely cause | Next tool |
|---------|--------------|-----------|
| High ready ms on first visit only | Lazy page ctor / service resolve | CPU profiler + Allocation |
| High ready ms every visit | Heavy `OnNavigatedTo` / data refresh | CPU profiler, disable sensors temporarily |
| Large Δ working set on one page | Image caches, sensor history, list virtualization missing | Allocation profiler, Live Visual Tree |
| High UIA node count | Dense tree / no virtualization | Inspect ItemsControl virtualization |
| Backend WMI slow | Hardware query on UI thread | Move to background + cache |

## Notes

- Close other UDT instances before a timed run (`UiPerformance.Smoke` kills them by default).
- Prefer **Release | x64** for numbers you care about.
- Sandbox app data is used (`UDT_APPDATA_OVERRIDE`) so production settings are not touched.
- Keyboard page may be absent on non-Legion hardware; the harness still records success if the “no compatible keyboards” empty state appears quickly.
