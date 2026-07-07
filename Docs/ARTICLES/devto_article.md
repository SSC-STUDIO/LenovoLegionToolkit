---
title: "Building a Lightweight Vantage Alternative with .NET 10 and WPF"
published: false
description: "How we built Universal Device Toolkit ¡ª a GPL-3.0 open-source hardware control app for Legion laptops with a plugin system, 78+ language localizations, and 2,500+ unit tests ¡ª all in C# and WPF on .NET 10."
tags: dotnet, wpf, csharp, opensource
canonical_url: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
---

# Building a Lightweight Vantage Alternative with .NET 10 and WPF

Lenovo's Vantage app ships with every Legion laptop. It requires a background service, an account, and telemetry. For a lot of us, that's a dealbreaker ¡ª we just want to switch Fn+Q modes, set a fan curve, and move on.

**Universal Device Toolkit** ([GitHub](https://github.com/SSC-STUDIO/UniversalDeviceToolkit)) is the open-source answer. It's a GPL-3.0 Windows desktop app built in C# / WPF on .NET 10 that replaces Vantage's core hardware controls with something lightweight, private, and extensible via plugins.

This post covers the engineering behind it ¡ª the decisions that matter, the pitfalls we hit, and the patterns that held up under a78-language localization and2,500+ unit tests.

![UDT Main Interface](https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit/master/Assets/Screenshot_main.png)

## Why Another Hardware Tool?

Lenovo Legion Toolkit existed before UDT, but it was locked to Legion-only machines and its plugin model was limited. When users on LOQ, IdeaPad Gaming, or even non-Lenovo PCs wanted basic system tools, there was nothing.

UDT widens the scope:

- **Full hardware control** on supported Legion/LOQ/IdeaPad Gaming models
- **Basic mode** on any other PC (plugins, themes, optimization ¡ª hardware toggles hidden)
- **Plugin extensions** as the primary expansion path ¡ª not a bolt-on afterthought

The constraint: keep the same performance profile. UDT should use less memory than Vantage, start faster, and not require admin unless it genuinely needs hardware access.

## Architecture at a Glance

```
UniversalDeviceToolkit.WPF   ¡û MVVM presentation (pages, controls, styles)
UniversalDeviceToolkit.Lib   ¡û 34 hardware controllers, services, game detection
UniversalDeviceToolkit.CLI   ¡û scripting/automation (llt.exe)
UniversalDeviceToolkit.Lib.Plugins  ¡û plugin SDK & host
UniversalDeviceToolkit.Lib.Automation  ¡û automation rules engine
UniversalDeviceToolkit.Lib.Macro  ¡û macro recording/playback
```

Everything runs in a single process. There is no background service. The app stays in the tray by design (it syncs Fn+Q and macros), but it does not spawn background workers after initialization.

Key technology choices:

| Layer | Choice | Why |
|---|---|---|
| UI | WPF | Native Windows, GPU-accelerated, XAML for theme binding |
| DI | Autofac | Module-based registration, supports plugin isolation |
| Hardware | WMI + ACPI + HID | Direct firmware control, no cloud dependency |
| Monitoring | LibreHardwareManagerLib | Cross-vendor sensor access |
| Localization | Crowdin + `.resx` |78+ languages, community-managed |
| Testing | xUnit + FlaUI + WinRT OCR | Unit, integration, and visual verification |

## The Three Engineering Challenges That Mattered Most

### 1. WMI Deadlocks Under RDP

**The problem:** Synchronous `ManagementObjectSearcher.Get()` calls enter tight ACPI spinloops when a session is connected via Remote Desktop. The UI freezes for30 seconds.

**The fix:** Every WMI query in the codebase now goes through an async extension method with a hard2,500ms timeout:

```csharp
// Before (dangerous)
var searcher = new ManagementObjectSearcher(query);
var results = searcher.Get();  // blocks UI thread, can deadlock under RDP

// After (safe)
var results = await searcher.GetAsync(timeoutMs: 2500);
```

We enforced this as a project rule: **zero synchronous WMI calls** in any code path reachable from the UI thread. The reporter scans for this pattern on every pass.

### 2. WPF Thread Safety and `.ConfigureAwait(false)`

**The problem:** Someone adds `.ConfigureAwait(false)` to a ViewModel async call, thinking it's a performance improvement. In WPF, it strips the `SynchronizationContext`, and the next UI property access throws `InvalidOperationException` on a background thread.

**The fix:** The rule is simple ¡ª **zero `.ConfigureAwait(false)` in WPF UI or ViewModel code**. The only exception is `App.xaml.cs` background initialization, where the continuation explicitly returns to the UI via `Dispatcher.InvokeAsync()`.

For background service callbacks that need to update the UI, we require explicit `Dispatcher.CheckAccess()` guards:

```csharp
// Background service callback ¡ª must guard
void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
{
    if (_dispatcher.CheckAccess())
    {
        RefreshUI();
    }
    else
    {
        _dispatcher.InvokeAsync(RefreshUI);
    }
}
```

### 3.78-Language Localization Without Hardcoded Strings

**The problem:** WPF developers love writing `Text="OK"` directly in XAML. With78+ language files, one hardcoded English string breaks the experience for everyone else.

**The fix:** Every user-facing string must reference `Resource.resx` via strongly-typed keys:

```xml
<!-- Before -->
<Button Content="OK" />

<!-- After -->
<Button Content="{x:Static resources:Resource.OK}" />
```

The reporter scans every XAML file and C# file for hardcoded UI text. Resource key naming follows a strict namespace: `<Area>_<Component>_<Purpose>` (e.g., `SettingsPage_AutoUpdate_Header`). Format strings use indexed placeholders (`{0}`, `{1}`) ¡ª never concatenation.

## The Plugin System

Plugins are first-class citizens, not an afterthought:

```plaintext
plugins/
©À©¤©¤ manifest.json      ¡û metadata, dependencies, sandbox rules
©À©¤©¤ plugin.dll         ¡û main plugin assembly
©À©¤©¤ [dependencies]     ¡û additional assemblies
©¸©¤©¤ [resources]        ¡û plugin resources
```

Plugin types:

- **Feature plugins**: Add new automation features to the main app
- **Integration plugins**: Connect third-party services
- **Tool plugins**: Standalone utilities (CPU tuning, GPU info, network tools)

Plugins can be installed, updated, configured, and removed from the Plugin Extensions page inside the app. The host provides a sandboxed load context with dependency resolution and manifest checks.

Non-Lenovo machines run plugins, themes, and system optimization in basic mode while hiding unsupported hardware toggles. The "Universal" in the name is about extensibility, not a promise of full hardware control on every PC brand.

## Testing Strategy:2,500+ Tests

The test suite has three tiers:

1. **Unit tests** (2,327 passing): Controller logic, service behavior, utility functions, plugin loading
2. **Plugin tests** (186 passing): Plugin SDK contract verification, dependency resolution
3. **Cross-platform tests** (119 passing): Platform-independent code paths

FlaUI-based UI smoke tests verify the app launches, renders, and exposes expected control structure. These run with auto-elevation for hardware access and skip gracefully when no desktop session is available.

```csharp
// Example: OCR-based UI verification
[Fact]
public async Task MainWindow_CanExtractVisibleText()
{
    var window = await LaunchAppAsync();
    var ocrText = await WinRtOcrHelper.ExtractTextAsync(window);
    Assert.Contains("Universal Device Toolkit", ocrText);
}
```

CI runs on both Linux (build + unit tests) and Windows (full integration + FlaUI).

## Performance Numbers

| Metric | Value |
|---|---|
| Idle memory | ~50¨C100 MB |
| Idle CPU | < 1% |
| Startup time | < 2 seconds |
| Background service | None |
| Telemetry | None |

For comparison, Lenovo Vantage typically uses200¨C400 MB and requires a background service.

## Localization Infrastructure

UDT uses [Crowdin](https://crowdin.com/) for community-managed translations:

- Source files: neutral `Resource.resx` in 4 modules (WPF, Lib, Automation, Macro)
- Target files: `Resource.<locale>.resx` beside each source file
- Locale mapping defined in `crowdin.yml`

Currently152 `.resx` files covering78+ locales. The CI verifies that all resource keys are present across all locales ¡ª a missing key is a build failure, not a runtime fallback.

## How to Contribute

1. Install [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. Clone: `git clone https://github.com/SSC-STUDIO/UniversalDeviceToolkit.git`
3. Build: `dotnet build UniversalDeviceToolkit.sln`
4. Run tests: `dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj`
5. See [CONTRIBUTING.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/CONTRIBUTING.md) for guidelines

For plugin development, see [PLUGIN_DEVELOPMENT.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/Docs/PLUGIN_DEVELOPMENT.md).

## Links

- **GitHub**: [https://github.com/SSC-STUDIO/UniversalDeviceToolkit](https://github.com/SSC-STUDIO/UniversalDeviceToolkit)
- **Releases**: [https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest)
- **Install (Scoop)**: `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket; scoop install ssc-studio/lenovolegiontoolkit`
- **License**: GPL-3.0

---

*If UDT helps your Legion run leaner, a star helps more people find it ¡ª and tells us the plugin model is worth building out.*
