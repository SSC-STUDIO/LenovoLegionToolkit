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

| Rating | Median ready time |
|--------|-------------------|
| excellent | ≤ 400 ms |
| good | ≤ 900 ms |
| fair | ≤ 1800 ms |
| needs work | > 1800 ms |

## Analysis tools (use together)

### 1. UiPerformance.Smoke (automated baseline)

- UI Automation navigation timing
- Working set / private bytes / handles per surface
- Approximate UIA tree size (complexity proxy)

### 2. UniversalDeviceToolkit.PerformanceTest (backend)

```powershell
dotnet run --project UniversalDeviceToolkit.PerformanceTest -c Release
```

Measures logging, WMI, file IO, settings load, collections — useful when a page is slow due to service/init cost rather than XAML.

### 3. Visual Studio Performance Profiler

1. Open `UniversalDeviceToolkit.WPF`
2. **Debug → Performance Profiler** (Alt+F2)
3. Enable:
   - **CPU Usage** — find expensive page `OnNavigatedTo` / sensor refresh
   - **.NET Object Allocation** — find allocation spikes on navigate
   - **UI Analysis** (when available) — layout/render stalls
4. Navigate every sidebar item while recording
5. Stop and inspect hottest stacks by page transition

### 4. `dotnet-counters` (live runtime)

```powershell
dotnet tool install -g dotnet-counters   # once

# While UDT is running:
dotnet-counters monitor --process-id <pid> `
  System.Runtime `
  Microsoft.Windows.Desktop.App.WPF
```

Watch: `Time in GC`, `Allocation Rate`, `Working Set`, WPF frame / UI thread indicators.

### 5. `dotnet-trace` / PerfView (deep stacks)

```powershell
dotnet tool install -g dotnet-trace
dotnet-trace collect -p <pid> --duration 00:01:00 --providers Microsoft-Windows-DotNETRuntime
```

Or PerfView:

```text
PerfView /nogui collect /MaxCollectSec:60
# flip every page, then open the .etl.zip in PerfView → CPU Stacks / GC Stats
```

### 6. Visual Regression smoke (pixel + load path)

```powershell
dotnet run --project Tools\VisualRegression.Smoke -c Release -p:Platform=x64 -- `
  --repo-root . --out _visual_smoke_out --configuration Release
```

Not a timer, but exercises the same navigation graph and catches visual jank from missing resources.

### 7. FlaUI automated tests

```powershell
dotnet test UniversalDeviceToolkit.UiAutomation.Tests/UniversalDeviceToolkit.UiAutomation.Tests.csproj --framework net10.0-windows10.0.26100.0 --configuration Release --filter "FullyQualifiedName~FlaUI"
```

See [FlaUI_Testing.md](./FlaUI_Testing.md).

### 8. Windows-hosted Linux verification through WSL

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
