# Plugin SDK Boundary

This document defines the public contract surface that third-party plugins are
allowed to depend on at compile time. Everything else in
`UniversalDeviceToolkit.Lib.Plugins` is **host-internal** and may change between
host versions without notice.

The split mirrors the assembly configuration in
`UniversalDeviceToolkit.Lib.Plugins.csproj`: the SDK source files were removed
from this repository entirely (the host build no longer compiles them) and
ship as pre-built binaries (`UniversalDeviceToolkit.Plugins.SDK.dll` /
`UniversalDeviceToolkit.Plugins.Shared.dll`) produced by the external
`Plugins/SDK/Runtime` and `Plugins/Shared`, copied into the app output by
`UniversalDeviceToolkit.WPF.csproj`. Plugins only see the SDK surface.

In addition, `UniversalDeviceToolkit.Lib\Plugins\LegacyPluginContracts.cs`
carries an in-Lib legacy mirror of the same contracts (same namespace,
different assembly) so older plugins that reference the Lib assembly remain
ABI-compatible. Both surfaces are frozen — see the header comments in those
two files.

## Public SDK (safe for plugins to reference)

These types form the stable, supported contract. Plugins may reference them
directly, implement the interfaces, and subclass the base class. The host
guarantees backward compatibility for the public surface listed here within a
major host version.

### Core contracts

| Type | Kind | Purpose |
| --- | --- | --- |
| `IPlugin` | interface | Required contract every plugin implements. |
| `PluginBase` | abstract class | Optional convenience base with default `IPlugin` implementations and access to `Configuration` / `HostContext`. |
| `IAppStartupPlugin` | interface | Optional lifecycle hook invoked once after the host has finished loading plugins. |
| `IOptimizationCategoryProvider` | interface | Optional contract for plugins that contribute Windows optimization categories. |

### Configuration & state

| Type | Kind | Purpose |
| --- | --- | --- |
| `IPluginConfiguration` | interface | Key/value configuration store scoped to the plugin. |
| `PluginConfiguration` | class | Default `IPluginConfiguration` implementation backed by a JSON file in the host's app-data directory. |
| `IPluginHostContext` | interface | Read-only access to host services (mode, owner window, dialog, settings). |
| `PluginHostContext` | static class | Global accessor exposing the currently active `IPluginHostContext`. Always non-null (falls back to a no-op). |
| `PluginHostMode` | enum | `Preview` / `RealRuntime` mode flag exposed by the host. |
| `PluginState` | enum | Lifecycle state values (`NotInstalled`, `Installed`, `Enabled`, `Disabled`, `Error`). Plugins observe this but never mutate it directly. |
| `PluginStateChangedEventArgs` | class | Event payload for the `PluginStateChanged` event. |
| `PluginConstants` | static class | Well-known plugin identifiers shipped by the host. |

### UI extension points

| Type | Kind | Purpose |
| --- | --- | --- |
| `IPluginPage` | interface | Plugins that need to inject a host-rendered page implement this and return it from `PluginBase.GetFeatureExtension` / `GetSettingsPage`. |

### Service-locator contract

Plugins resolve host-side services through the static `PluginHostContext.Current`
property and (where the host provides it) constructor parameters passed to
`IPlugin` implementations. The SDK does **not** expose any IoC container type
to plugins.

## Host-internal (DO NOT reference from plugins)

The following types live inside `UniversalDeviceToolkit.Lib.Plugins` for the
host's own use. They are not part of the SDK and plugins must not take a
compile-time reference to them. If a plugin needs functionality that only these
types appear to provide, request a stable SDK addition through the host's
plugin-author channels.

### Lifecycle & registry

- `IPluginManager` / `PluginManager`
- `IPluginRegistry` / `PluginRegistry`
- `IPluginLoader` / `PluginLoader`
- `IPluginFileSystemManager` / `PluginFileSystemManager`
- `IPluginHotReload` / `PluginHotReload`
- `PluginInstallationService`
- `PluginRepositoryService`
- `PluginLifecycleStateMachine`
- `PluginRegistry` internals (`ReplaceWithMetadataAdapter`, `MarkStarted`, `MarkStopped`)

### Security & signing

- `IPluginSignatureValidator` / `PluginSignatureValidator`
- `PluginSignatureSettings`, `PluginSignatureResult`, `PluginSignatureStatus`
- `TrustedPluginPackageStore`
- `PathSecurity` (private to the host; plugins must never assume they can
  reach the file system freely)
- `VersionChecker`

### Packaging & storage

- `PluginManifest`, `PluginManifestAdapter`
- `PluginMetadata`
- `PluginPaths`
- `PluginFileSystemManager`
- `PluginUiCapabilityResolver`
- `OptimizationCategoryExtender` (host-side `IOptimizationCategoryExtender`)
- `IOptimizationCategoryExtender`

### Sandbox

- `IPluginSandbox` / `PluginSandbox`
- `SandboxPermission`, `SandboxConfiguration`, `SandboxedPluginInfo`,
  `SandboxOperationResult`, `SandboxResourceUsage`, `ResourceType`,
  `SandboxViolationEventArgs`, `ResourceLimitExceededEventArgs`
- `PluginStateData`, `HotReloadConfiguration`, `HotReloadEventArgs`,
  `HotReloadResult`, `IStatefulPlugin`

### Events & diagnostics

- `PluginEventArgs` (used by the host-only `IPluginManager.PluginStateChanged`
  event; plugins should rely on `IPlugin.OnInstalled` / `OnUninstalled`
  callbacks instead)
- `PluginHealthStatus`
- `IoCModule` (Autofac registration module; the SDK surface uses
  `PluginHostContext` instead)
- `DependencyResolver` / `IDependencyResolver`

### Reflection helpers and constants

- `PluginConstants` is **public** but its values are reserved for host-shipped
  plugins (e.g. `NetworkAcceleration`, `ViveTool`); third-party plugins must
  not use these identifiers.

## Versioning & compatibility rules

1. The host promises that any type listed under **Public SDK** will not be
   removed or have its binary signature changed within a host major version
   (e.g. `2.x.x` → `2.y.y`).
2. New members may be added to public interfaces; plugins should provide
   default implementations (e.g. via `PluginBase`) and not implement SDK
   interfaces directly when a base class is available.
3. Anything under **Host-internal** may change at any time. Plugins that
   reflect over or reference these types will break across host updates.
4. The SDK is shipped in two assemblies that the host's
   `PluginAssemblyLoadContext` treats as shared:

   - `UniversalDeviceToolkit.Plugins.SDK.dll` — interfaces, base classes, and
     stable enums.
   - `UniversalDeviceToolkit.Plugins.Shared.dll` — default implementations
     plugins can choose to consume (e.g. `PluginConfiguration`).

   Plugins that need to extend a host-internal type must propose a new SDK
   type instead of referencing the existing one.

## How the boundary is enforced

- The host does not compile the SDK sources itself: the SDK surface is
  consumed exclusively through the pre-built `Plugins.SDK` /
  `Plugins.Shared` assemblies produced by the in-tree Plugins solution
  and copied into the app output by the host's csproj.
- `[assembly: InternalsVisibleTo("UniversalDeviceToolkit.Tests")]` in
  `AssemblyInfo.cs` grants the test project access to internal members for
  white-box testing. Plugins are never granted this access.
- A plugin taking a reference to a host-internal type will not be loadable
  through the signed SDK contract and may be rejected at load time by the
  signature validator and assembly load context.
