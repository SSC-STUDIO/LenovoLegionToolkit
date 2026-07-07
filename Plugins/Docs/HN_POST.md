# Show HN: Universal Device Toolkit Plugins – Open-source WPF plugin ecosystem for Windows

**URL**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins

**Title**: Show HN: Universal Device Toolkit Plugins – Open-source WPF plugin ecosystem for Windows (.NET 10)

**Submission text**:

I built a plugin ecosystem for Windows device management using C# .NET 10 and WPF-UI (Fluent Design). Think VS Code extensions, but for your Windows device toolkit.

The repo has 5 official plugins:

- Network Acceleration – real-time network telemetry, gaming presets, one-click optimization
- Custom Mouse – DPI profiles, theme-aware cursor styles, pointer speed management
- ViVeTool GUI – unlock hidden Windows feature flags from a searchable table (no CLI needed)
- Shell Integration – right-click context menu integration for power features
- Battery Health – battery health monitoring, cycle count, capacity degradation

Technical highlights:
- Zero warnings across 7 C# projects (TreatWarningsAsErrors=true)
- 409 unit tests, all passing
- SettingsManager with SaveWithDebounce (62ms → 0ms latency, 97% I/O reduction)
- Zero hardcoded colors – all UI uses DynamicResource theme bindings
- Custom plugin SDK with fallback UI pattern
- PluginWorkbench standalone preview tool (no host app required for testing)
- MIT License, no ads, no telemetry

The plugin architecture separates concerns cleanly: SDK defines interfaces (IPlugin, IPluginFeaturePage, IPluginSettingsPage), plugins implement them, and the host loads via reflection. Each plugin has a BuildFallbackUi() method that constructs the entire UI programmatically as a safety net.

Feedback welcome – especially on the plugin SDK design and the fallback UI pattern.
