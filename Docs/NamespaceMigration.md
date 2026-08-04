# Namespace and Assembly Migration

This document records the **completed Phase 3 hard cutover** from Lenovo Legion Toolkit (LLT) binary/namespace ABI to **Universal Device Toolkit (UDT)**, plus the **remaining legacy compatibility surfaces** that intentionally still contain LLT / Lenovo tokens.

Related: [ARCHITECTURE.md](./ARCHITECTURE.md), [PLUGIN_DEVELOPMENT.md](./PLUGIN_DEVELOPMENT.md), CHANGELOG brand/ABI cutover notes.

---

## Status summary

| Phase | Scope | Status |
| --- | --- | --- |
| **0** | User-facing brand (product name, WPF process, installer, AppData path migration) | **Done** |
| **1** | Conventions for new non-ABI host code → `UniversalDeviceToolkit.*` | **Done** |
| **2** | Non-breaking dual surfaces (IPC pipes, `BrandCompatibility`, automation env aliases) | **Done** |
| **3** | Hard cutover: Lib / Lib.Plugins `AssemblyName` + C# namespaces + Windows CLI exe name | **Done** |

Phase 3 is **complete in this repository**. New host code should use `UniversalDeviceToolkit.*` identities. Do **not** reintroduce `LenovoLegionToolkit.Lib*` as the primary assembly or namespace contract.

---

## Current state (from `.csproj`)

Values below are explicit `RootNamespace` / `AssemblyName` when set. If `AssemblyName` is omitted, the MSBuild default is the project file name (folder/project stem).

### Plugin / host contracts (post–Phase 3 primary ABI)

| Project folder | RootNamespace | AssemblyName | Role |
| --- | --- | --- | --- |
| `UniversalDeviceToolkit.Lib` | `UniversalDeviceToolkit.Lib` | `UniversalDeviceToolkit.Lib` | Core library; plugin and host type identity |
| `UniversalDeviceToolkit.Lib.Plugins` | `UniversalDeviceToolkit.Lib.Plugins` | `UniversalDeviceToolkit.Lib.Plugins` | Host plugin surface loaded with core Lib |

C# types under these projects live in `UniversalDeviceToolkit.Lib*` namespaces (renamed from `LenovoLegionToolkit.Lib*`). Official plugins under `Plugins/Official/` should target `UniversalDeviceToolkit.Plugins.*` / SDK Shared against these identities.

### Other projects

| Project folder | RootNamespace | AssemblyName (if set) | Notes |
| --- | --- | --- | --- |
| `UniversalDeviceToolkit.WPF` | `UniversalDeviceToolkit.WPF` | `Universal Device Toolkit` | Shipping UI; process name is the brand |
| `UniversalDeviceToolkit.Lib.Automation` | `UniversalDeviceToolkit.Lib.Automation` | *(project default)* | Not a public plugin ABI assembly |
| `UniversalDeviceToolkit.Lib.Macro` | `UniversalDeviceToolkit.Lib.Macro` | *(project default)* | Not a public plugin ABI assembly |
| `UniversalDeviceToolkit.CLI` | `UniversalDeviceToolkit.CLI` | **`udt-cli`** | Windows CLI executable → `udt-cli.exe` (was `llt`) |
| `UniversalDeviceToolkit.CLI.Lib` | `UniversalDeviceToolkit.CLI.Lib` | *(project default)* | Shared CLI IPC models |
| `UniversalDeviceToolkit.NetworkProxy` | `UniversalDeviceToolkit.NetworkProxy` | `UniversalDeviceToolkit.NetworkProxy` | Fully UDT-named |
| `UniversalDeviceToolkit.CrossPlatform` | `UniversalDeviceToolkit.CrossPlatform` | **`udt`** | Cross-platform CLI entry name |
| `UniversalDeviceToolkit.Tests` | `UniversalDeviceToolkit.Tests` | *(project default)* | Tests |
| `UniversalDeviceToolkit.CrossPlatform.Tests` | `UniversalDeviceToolkit.CrossPlatform.Tests` | *(project default)* | Tests |
| `UniversalDeviceToolkit.SpectrumTester` | `UniversalDeviceToolkit.SpectrumTester` | `SpectrumTester` | Dev tool |
| `UniversalDeviceToolkit.PerformanceTest` | `UniversalDeviceToolkit.PerformanceTest` | *(project default)* | Dev tool |

Tools under `Tools/` use their own small RootNamespaces (`HardwareValidation`, smoke tools, etc.) and are out of the public ABI surface.

### NetworkProxy

`UniversalDeviceToolkit.NetworkProxy` uses:

- `RootNamespace` = `UniversalDeviceToolkit.NetworkProxy`
- `AssemblyName` = `UniversalDeviceToolkit.NetworkProxy`

IPC defaults (e.g. pipe base name `udt-network-proxy`) are UDT-oriented. No rename work is required for this project.

---

## What Phase 3 changed

| Surface | Before (LLT) | After (UDT primary) |
| --- | --- | --- |
| Lib `RootNamespace` / `AssemblyName` | `LenovoLegionToolkit.Lib` | `UniversalDeviceToolkit.Lib` |
| Lib.Plugins `RootNamespace` / `AssemblyName` | `LenovoLegionToolkit.Lib.Plugins` | `UniversalDeviceToolkit.Lib.Plugins` |
| C# namespaces in Lib* | `LenovoLegionToolkit.Lib*` | `UniversalDeviceToolkit.Lib*` |
| Windows CLI `AssemblyName` | `llt` → `llt.exe` | `udt-cli` → `udt-cli.exe` (+ release ships `llt.exe` copy as one-train shim) |
| Preferred CLI IPC pipe | (introduced in Phase 2) | `UniversalDeviceToolkit-IPC-0` |
| Legacy CLI IPC pipe | `LenovoLegionToolkit-IPC-0` | **Still accepted** (compat) |
| winget `PackageIdentifier` | `SSC-STUDIO.LenovoLegionToolkit` | **Unchanged** (in-place upgrades; do not rewrite historical manifests) |
| Compatibility installer alias | `LenovoLegionToolkit_v*_Setup.exe` | **Still published** (copy of Full setup) |

**Impact:** Plugins and tools compiled only against pre-cutover `LenovoLegionToolkit.Lib*` assembly/type identities **will not bind** to `UniversalDeviceToolkit.Lib.dll` without rebuild. There is **no TypeForwardedTo shim** in this train. Host dual-load covers **filename prefixes** and dual-staged SDK/Shared DLL names only — not old Lib type identities.

---

## Remaining legacy compatibility surfaces

Phase 3 did **not** erase every Lenovo / LLT token. The following remain on purpose. **Do not delete them in drive-by cleanups** without a separate cutover plan.

### 1. Dual IPC named pipes (host ↔ CLI)

| Constant | Value | Role |
| --- | --- | --- |
| `Constants.PREFERRED_PIPE_NAME` | `UniversalDeviceToolkit-IPC-0` | Preferred / client-first |
| `Constants.DEFAULT_PIPE_NAME` | `LenovoLegionToolkit-IPC-0` | Legacy primary; still listened for older CLI / tooling |

- **Server (`IpcServer`)**: accept loops on both names (`Constants.GetServerPipeNames` — legacy then preferred for listen set).
- **Client (`IpcClient`)**: try preferred UDT first, then fall back to legacy (`GetClientPipeNames`).
- Isolation-path hashing suffixes **both** names the same way.

### 2. `BrandCompatibility` legacy constants

`UniversalDeviceToolkit.Lib/Branding/BrandCompatibility.cs` (namespace `UniversalDeviceToolkit.Lib.Branding`):

| Constant | Value | Role |
| --- | --- | --- |
| `ProductDisplayName` / `ProductCompactName` | Universal Device Toolkit / UniversalDeviceToolkit | Current brand |
| `LegacyProductDisplayName` / `LegacyProductCompactName` | Lenovo Legion Toolkit / LenovoLegionToolkit | Migration messaging, AppData migration, docs |
| `PreferredAssemblyLib` / `PreferredAssemblyLibPlugins` | `UniversalDeviceToolkit.Lib` / `UniversalDeviceToolkit.Lib.Plugins` | **Current** primary assembly simple names |
| `LegacyAssemblyLib` / `LegacyAssemblyLibPlugins` | `LenovoLegionToolkit.Lib` / `LenovoLegionToolkit.Lib.Plugins` | Messaging / detection only — **not** a runtime bind target |

Also see `AppIdentity` legacy display/compact/repository tokens used for AppData migration and historical links.

### 3. Plugin assembly prefixes (transition)

Host plugin discovery/install/load should accept:

- **Preferred:** `UniversalDeviceToolkit.Plugins.*`
- **Legacy:** `LenovoLegionToolkit.Plugins.*`

SDK/Shared dual-stage (`PluginAssemblyNaming.StageDualNamed*`) copies the same bytes under **both** UDT and LLT filenames so either simple name can resolve from the plugin folder. Official plugins must still **recompile** against `UniversalDeviceToolkit.Lib*` / `UniversalDeviceToolkit.Plugins.SDK`.

New plugins and packaging should ship under `UniversalDeviceToolkit.Plugins.*`.

### 4. Automation environment variables (dual-write)

`AutomationEnvironment` dual-writes each automation value under **primary `UDT_*`** and **alias `LLT_*`** (`ToLltAlias`). Scripts may read either prefix.

| Legacy (`LLT_*`) | UDT alias | Context |
| --- | --- | --- |
| `LLT_IS_AC_ADAPTER_CONNECTED` | `UDT_IS_AC_ADAPTER_CONNECTED` | Automation trigger env |
| `LLT_IS_AC_ADAPTER_LOW_POWER` | `UDT_IS_AC_ADAPTER_LOW_POWER` | Automation trigger env |
| `LLT_IS_DISPLAY_ON` | `UDT_IS_DISPLAY_ON` | Automation trigger env |
| `LLT_IS_EXTERNAL_DISPLAY_CONNECTED` | `UDT_IS_EXTERNAL_DISPLAY_CONNECTED` | Automation trigger env |
| `LLT_IS_GAME_RUNNING` | `UDT_IS_GAME_RUNNING` | Automation trigger env |
| `LLT_IS_HDR_ON` | `UDT_IS_HDR_ON` | Automation trigger env |
| `LLT_IS_LID_OPEN` | `UDT_IS_LID_OPEN` | Automation trigger env |
| `LLT_STARTUP` | `UDT_STARTUP` | Automation trigger env |
| `LLT_RESUME` | `UDT_RESUME` | Automation trigger env |
| `LLT_POWER_MODE` | `UDT_POWER_MODE` | Automation trigger env |
| `LLT_POWER_MODE_NAME` | `UDT_POWER_MODE_NAME` | Automation trigger env |
| `LLT_PROCESSES_STARTED` | `UDT_PROCESSES_STARTED` | Automation trigger env |
| `LLT_PROCESSES` | `UDT_PROCESSES` | Automation trigger env |
| `LLT_DEVICE_CONNECTED` | `UDT_DEVICE_CONNECTED` | Automation trigger env |
| `LLT_DEVICE_INSTANCE_IDS` | `UDT_DEVICE_INSTANCE_IDS` | Automation trigger env |
| `LLT_IS_SUNSET` | `UDT_IS_SUNSET` | Automation trigger env |
| `LLT_IS_SUNRISE` | `UDT_IS_SUNRISE` | Automation trigger env |
| `LLT_TIME` | `UDT_TIME` | Automation trigger env |
| `LLT_DAYS` | `UDT_DAYS` | Automation trigger env |
| `LLT_PERIOD` | `UDT_PERIOD` | Automation trigger env |
| `LLT_IS_USER_ACTIVE` | `UDT_IS_USER_ACTIVE` | Automation trigger env |
| `LLT_WIFI_CONNECTED` | `UDT_WIFI_CONNECTED` | Automation trigger env |
| `LLT_WIFI_SSID` | `UDT_WIFI_SSID` | Automation trigger env |
| `LLT_SESSION_LOCKED` | `UDT_SESSION_LOCKED` | Automation trigger env |
| `LLT_LOG_PATH` | *(legacy-only today)* | Set by WPF startup (`StartupOrchestrator`); not dual-aliased |
| `LLT_PLUGIN_SIGNATURE_MODE` | *(docs / smoke tooling)* | Plugin signature policy override; not an automation env key |

Do **not** remove `LLT_*` keys without a dedicated script-migration notice.

### 5. Packaging / distribution identifiers (transition)

- **Scoop** bucket id may still use historical tokens (e.g. `lenovolegiontoolkit`) for upgrade continuity.
- **winget** packaging folders under this repo historically live under `Packaging/winget/.../LenovoLegionToolkit/`; package identity in drafts/docs may use `SSC-STUDIO.UniversalDeviceToolkit` and/or legacy `SSC-STUDIO.LenovoLegionToolkit` depending on submission status — treat winget as **in transition**, not fully cleaned of Lenovo tokens.
- Crowdin project URLs and some external links may still say `llt`.

### 6. Other intentional Lenovo references

Device-support catalogs, hardware series enums (e.g. Legion families), driver/package names, and user-facing copy about Lenovo hardware are **product features**, not brand/ABI leftovers.

---

## Historical phases (archive)

### Phase 0 — Done: user-facing brand UDT

Product name, WPF `AssemblyName` (`Universal Device Toolkit`), assets, packaging Full/Online names, AppData path migration, env/docs brand copy.

### Phase 1 — Done: conventions for new code

New host modules that are not part of the public plugin contract use `UniversalDeviceToolkit.*` namespaces and project naming.

### Phase 2 — Done: dual surfaces without hard ABI rename

| Dual surface | Status | Notes |
| --- | --- | --- |
| Dual IPC named pipes | **Shipped** | Preferred `UniversalDeviceToolkit-IPC-0` + legacy `LenovoLegionToolkit-IPC-0` |
| `BrandCompatibility` constants | **Shipped** | Display names + preferred/legacy assembly simple names |
| Automation env vars `LLT_*` / `UDT_*` | **Shipped (aliases)** | Dual-write via `ToUdtAlias` |

### Phase 3 — Done: hard cutover

- `AssemblyName` / `RootNamespace` for Lib and Lib.Plugins → `UniversalDeviceToolkit.Lib` / `UniversalDeviceToolkit.Lib.Plugins`
- Source namespaces `LenovoLegionToolkit.Lib*` → `UniversalDeviceToolkit.Lib*`
- Windows CLI `AssemblyName` `llt` → `udt-cli`
- Primary plugin/host contract is UDT-named; legacy plugin prefixes and pipes remain as **compat**, not primary ABI

TypeForwardedTo dual-package packaging for **external** consumers of the old assembly simple names was **not** required for this in-tree hard cutover; third-party plugins built against `LenovoLegionToolkit.Lib*` must recompile against `UniversalDeviceToolkit.Lib*` (or rely only on remaining host-side name tolerances where they apply).

---

## CLI and IPC compatibility notes

| Concern | Current value | Guidance |
| --- | --- | --- |
| CLI `AssemblyName` | `udt-cli` (`UniversalDeviceToolkit.CLI`) | Ship/docs use `udt-cli.exe`; CrossPlatform uses `udt` |
| Named pipe — **preferred UDT** | `UniversalDeviceToolkit-IPC-0` (`PREFERRED_PIPE_NAME`) | Client-preferred; host dual-listens |
| Named pipe — **legacy** | `LenovoLegionToolkit-IPC-0` (`DEFAULT_PIPE_NAME`) | Older CLI / tooling; keep until a deliberate pipe-only cutover |
| Automation env vars | `LLT_*` + `UDT_*` dual-write | Compatibility surface for user scripts |
| Brand / assembly dual constants | `BrandCompatibility` | Preferred = current UDT assemblies; `Legacy*` = pre–Phase 3 LLT names |
| Network proxy pipe | `udt-network-proxy` (and session-suffixed variants) | UDT-native; separate from CLI IPC |

---

## Conventions for contributors and agents

1. **User-visible brand** → Universal Device Toolkit / UDT.
2. **Primary plugin/host ABI** → `UniversalDeviceToolkit.Lib` and `UniversalDeviceToolkit.Lib.Plugins` (assemblies **and** namespaces).
3. **New code** → `UniversalDeviceToolkit.*` namespaces; do not introduce new `LenovoLegionToolkit.*` type namespaces.
4. **Legacy compat only** → keep dual pipes, `BrandCompatibility.Legacy*`, `LLT_*` env dual-write, and legacy plugin prefixes until a **documented** removal pass.
5. **Do not claim “zero Lenovo tokens”** — residual compat strings and packaging paths are expected during transition.
6. **Plugin authors** → target `UniversalDeviceToolkit.Plugins.*` / current Lib assembly names; rebuild plugins after the Phase 3 host cutover if they still reference `LenovoLegionToolkit.Lib*`.

---

## Quick reference: primary vs legacy

```
Repo / solution:       UniversalDeviceToolkit.*
WPF process:           "Universal Device Toolkit"
Core Lib DLL:          UniversalDeviceToolkit.Lib.dll          (was LenovoLegionToolkit.Lib.dll)
Plugins host DLL:      UniversalDeviceToolkit.Lib.Plugins.dll  (was LenovoLegionToolkit.Lib.Plugins.dll)
Plugin prefixes:       UniversalDeviceToolkit.Plugins.* (preferred)
                       LenovoLegionToolkit.Plugins.*   (legacy accepted)
CLI exe (Windows):    udt-cli.exe   (was llt.exe)
CLI IPC pipes:         UniversalDeviceToolkit-IPC-0 (preferred)
                       LenovoLegionToolkit-IPC-0    (legacy)
Automation env:        LLT_* + UDT_* (dual-write)
Brand constants:       BrandCompatibility — Preferred* current; Legacy* LLT names
NetworkProxy:          UniversalDeviceToolkit.NetworkProxy (fully UDT)
```
