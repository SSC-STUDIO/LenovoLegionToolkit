---
title: "I Built a Plugin Ecosystem for Windows Device Management in C# .NET 10"
published: false
description: "How I architected 5 open-source WPF plugins with zero hardcoded colors, zero warnings, and 409 unit tests"
tags: csharp, dotnet, windows, opensource
canonical_url:
---

# I Built a Plugin Ecosystem for Windows Device Management in C# .NET 10

I've been working on **Universal Device Toolkit Plugins** — a free, open-source plugin ecosystem that extends [Universal Device Toolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit) (a lightweight alternative to OEM bloatware for Windows device management).

After a 10-day quality sprint, I shipped 5 production-ready plugins with **zero warnings**, **409 unit tests**, and a custom plugin SDK. Here's what I learned.

## The Architecture

The plugin system is inspired by VS Code extensions:

```
Plugin SDK (interfaces)
├── Plugins/
│   ├── BatteryHealth/      # Battery health monitoring
│   ├── CustomMouse/        # DPI profiles + cursor themes
│   ├── NetworkAcceleration/# Real-time network telemetry
│   ├── ShellIntegration/   # Right-click menu integration
│   └── ViveTool/           # Hidden Windows feature flags
├── Tools/
│   └── PluginWorkbench/    # Standalone preview tool
└── Dependencies/           # Shared + Host DLLs
```

Each plugin implements `IPlugin`, `IPluginFeaturePage`, and `IPluginSettingsPage` from the SDK. The host app loads them via reflection — plugins never reference the host directly.

### The Fallback UI Pattern

Every plugin has a `BuildFallbackUi()` method that programmatically constructs the entire UI when XAML loading fails. This means plugins work even if the XAML parser crashes:

```csharp
public UserControl? BuildFallbackUi()
{
    var panel = new StackPanel { Margin = new Thickness(16) };
    // ... build entire UI in code, zero XAML dependency
    return new UserControl { Content = panel };
}
```

This was critical for reliability — we've had zero UI-related crashes since implementing it.

## Theme System: Zero Hardcoded Colors

The biggest UI/UX win was eliminating all hardcoded hex colors. Every plugin now uses `DynamicResource` bindings:

```xml
<!-- Before: Hardcoded (breaks in dark mode) -->
<Border Background="#F5F5F5" CornerRadius="8">

<!-- After: Theme-aware -->
<Border Background="{DynamicResource ControlFillColorDefaultBrush}" CornerRadius="10">
```

The host app provides theme brushes through WPF's resource dictionary system. Plugins bind to tokens like `ControlFillColorDefaultBrush`, `TextFillColorPrimaryBrush`, and `CardBackgroundFillColorDefaultBrush`.

**Verification**: Automated CI grep for `#[0-9A-Fa-f]{6}` in all `*.xaml` files returns zero matches across all 5 plugins.

## SettingsManager: From 62ms to 0ms

The original `SettingsManager` saved to disk on every call. With 30 rapid saves, that's 30 I/O operations (62ms each).

The fix — `SaveWithDebounce()`:

```csharp
public bool SaveWithDebounce(T settings)
{
    lock (_lock)
    {
        _pendingSettings = settings;
        _saveDebounceTimer.Change(_debounceDelayMs, Timeout.Infinite);
        return true; // Returns immediately
    }
}
// Actual save happens 500ms after the LAST call
```

Combined with a memory transaction (skip save if settings unchanged), we achieved:

| Metric | Before | After |
|--------|--------|-------|
| Save latency | 62ms | 0ms (debounced) |
| Load latency | 2ms | 0ms (cached) |
| I/O ops (30 rapid saves) | 30 | 1 |

## Thread Safety: The ConfigureAwait(false) Trap

WPF plugins have a common gotcha: calling `.ConfigureAwait(false)` in UI code. This causes callbacks to run on thread pool threads, which crash when touching UI elements.

```csharp
// DANGEROUS in WPF code-behind
await SomeAsyncOperation().ConfigureAwait(false);
Dispatcher.Invoke(() => updateUI()); // Unnecessary hop

// SAFE
await SomeAsyncOperation(); // Captures SynchronizationContext
updateUI(); // Already on UI thread
```

The fix across the entire codebase: zero `ConfigureAwait(false)` in `*.xaml.cs` files. Background callbacks use `Dispatcher.InvokeAsync()` instead.

## Test Isolation: The Culture Race Condition

We hit a subtle bug: tests would randomly fail when run in parallel. The root cause? Static `CultureInfo.CurrentCulture` mutations in one test would affect another test reading localized strings.

The fix — `CollectionDefinition(DisableParallelization = true)` on every plugin test collection:

```csharp
[CollectionDefinition("BatteryHealthTests", DisableParallelization = true)]
public class BatteryHealthTestsCollectionDefinition { }
```

This serializes all tests within a plugin while still allowing cross-plugin parallelism. Result: **409/409 tests pass consistently**.

## The 5 Plugins

| Plugin | What it does | Tests |
|--------|-------------|-------|
| **Network Acceleration** | Real-time network telemetry, gaming presets, one-click optimization | 39 |
| **Custom Mouse** | Theme-aware cursor styles, DPI profiles, pointer speed management | 54 |
| **ViVeTool** | Browse and toggle hidden Windows feature flags from a searchable GUI | 186 |
| **Shell Integration** | Right-click context menu integration | 114 |
| **Battery Health** | Battery health monitoring, cycle count, capacity degradation | 16 |

## What's Next

The project is at v1.2.0-quality with 0 warnings across 7 C# projects. Future plans:

- **Plugin hot-reload** — switch plugins without restarting the host
- **Community plugins** — publish your own via the built-in plugin store
- **Cross-platform exploration** — core logic is in `Plugins/Shared/` (netstandard2.0 compatible)

## Try It Out

1. Install [Universal Device Toolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit)
2. Open **Plugins** → **Browse Store**
3. Install any plugin
4. Restart the application

Or build from source:

```powershell
git clone https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins
cd UniversalDeviceToolkit-Plugins
.\llt-plugin.cmd doctor    # Check environment
.\llt-plugin.cmd build     # Build all plugins
.\llt-plugin.cmd test      # Run 409 tests
```

---

**GitHub**: [SSC-STUDIO/UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins)

If you find this useful, a star on the repo helps more people discover it. PRs and plugin ideas welcome!
