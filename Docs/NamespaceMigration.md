# Namespace and Assembly Migration

This document describes the intentional split between **user-facing Universal Device Toolkit (UDT) branding** and **retained `LenovoLegionToolkit.*` binary/namespace ABI** for plugins and CLI compatibility.

> **Do not mass-rename** `LenovoLegionToolkit.Lib*` namespaces or assembly names without a documented migration (TypeForwardedTo / dual-package / coordinated plugin recompile). Full binary rename breaks third-party plugins until that story is complete.

Related: [ARCHITECTURE.md](./ARCHITECTURE.md), [PLUGIN_DEVELOPMENT.md](./PLUGIN_DEVELOPMENT.md), CHANGELOG brand-migration notes.

---

## Current state (from `.csproj`)

Values below are explicit `RootNamespace` / `AssemblyName` when set. If `AssemblyName` is omitted, the MSBuild default is the project file name (folder/project stem).

### ABI-retained (plugin / host contracts)

| Project folder | RootNamespace | AssemblyName | Role |
| --- | --- | --- | --- |
| `UniversalDeviceToolkit.Lib` | `LenovoLegionToolkit.Lib` | `LenovoLegionToolkit.Lib` | Core library; plugin and host type identity |
| `UniversalDeviceToolkit.Lib.Plugins` | `LenovoLegionToolkit.Lib.Plugins` | `LenovoLegionToolkit.Lib.Plugins` | Host plugin surface loaded with core Lib |

C# types under these projects live in `LenovoLegionToolkit.Lib*` namespaces. Official plugins (separate [UniversalDeviceToolkit-Plugins](https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins) repo) reference `LenovoLegionToolkit.Plugins.SDK` / Shared and compile against these identities.

### Migrated to UniversalDeviceToolkit (folder + RootNamespace)

| Project folder | RootNamespace | AssemblyName (if set) | Notes |
| --- | --- | --- | --- |
| `UniversalDeviceToolkit.WPF` | `UniversalDeviceToolkit.WPF` | `Universal Device Toolkit` | Shipping UI; process name is the brand |
| `UniversalDeviceToolkit.Lib.Automation` | `UniversalDeviceToolkit.Lib.Automation` | *(project default)* | Not a public plugin ABI assembly |
| `UniversalDeviceToolkit.Lib.Macro` | `UniversalDeviceToolkit.Lib.Macro` | *(project default)* | Not a public plugin ABI assembly |
| `UniversalDeviceToolkit.CLI` | `UniversalDeviceToolkit.CLI` | **`llt`** | CLI executable name kept short / LLT-compatible |
| `UniversalDeviceToolkit.CLI.Lib` | `UniversalDeviceToolkit.CLI.Lib` | *(project default)* | Shared CLI IPC models |
| `UniversalDeviceToolkit.NetworkProxy` | `UniversalDeviceToolkit.NetworkProxy` | `UniversalDeviceToolkit.NetworkProxy` | Already fully UDT-named (good) |
| `UniversalDeviceToolkit.CrossPlatform` | `UniversalDeviceToolkit.CrossPlatform` | **`udt`** | Cross-platform CLI entry name |
| `UniversalDeviceToolkit.Tests` | `UniversalDeviceToolkit.Tests` | *(project default)* | Tests |
| `UniversalDeviceToolkit.CrossPlatform.Tests` | `UniversalDeviceToolkit.CrossPlatform.Tests` | *(project default)* | Tests |
| `UniversalDeviceToolkit.SpectrumTester` | `UniversalDeviceToolkit.SpectrumTester` | `SpectrumTester` | Dev tool |
| `UniversalDeviceToolkit.PerformanceTest` | `UniversalDeviceToolkit.PerformanceTest` | *(project default)* | Dev tool |

Tools under `Tools/` use their own small RootNamespaces (`HardwareValidation`, smoke tools, etc.) and are out of the public ABI surface.

### NetworkProxy (already correct)

`UniversalDeviceToolkit.NetworkProxy` uses:

- `RootNamespace` = `UniversalDeviceToolkit.NetworkProxy`
- `AssemblyName` = `UniversalDeviceToolkit.NetworkProxy`

IPC defaults (e.g. pipe base name `udt-network-proxy`) are UDT-oriented. No rename work is required for this project.

---

## Why Lib / Lib.Plugins keep `LenovoLegionToolkit.Lib*` ABI

1. **Assembly identity**: Plugins load against `LenovoLegionToolkit.Lib.dll` / `LenovoLegionToolkit.Lib.Plugins.dll`. Changing `AssemblyName` changes the assembly simple name and breaks resolution unless every plugin is rebuilt and redistributed.
2. **Type identity**: .NET binds on assembly name + namespace + type name. Renaming `RootNamespace` / source namespaces without `TypeForwardedTo` (or equivalent dual packaging) invalidates compiled references in third-party and official plugins.
3. **Cross-repository contract**: The plugins repo and SDK (`LenovoLegionToolkit.Plugins.SDK`, Shared) are authored against these names. Host and plugin ecosystems must move together.
4. **CHANGELOG policy**: Brand migration already states that `LenovoLegionToolkit.*` assembly/namespace identifiers are **intentionally retained** as cross-repository ABI contracts for plugin loading.

User-facing strings, window titles, installer names, and process names are already **Universal Device Toolkit**. That is independent of binary ABI.

---

## Phased plan

### Phase 0 — Done: user-facing brand UDT

- Product name, WPF `AssemblyName` (`Universal Device Toolkit`), assets, packaging Full/Online names, AppData path migration notes, env/docs brand copy.
- Internal projects that are **not** plugin ABIs already use `UniversalDeviceToolkit.*` RootNamespaces (WPF, Automation, Macro, CLI source, NetworkProxy, CrossPlatform).

### Phase 1 — This PR: document + enforce conventions for **new** code

- This document is the source of truth for agents and contributors.
- **New** host modules that are not part of the public plugin contract should use `UniversalDeviceToolkit.*` namespaces and project naming.
- **Do not** “fix” Lib / Lib.Plugins by renaming `RootNamespace` / `AssemblyName` or bulk-rewriting `namespace LenovoLegionToolkit.Lib…`.
- Prefer XML comments on those csproj properties (see projects) so automated renames are discouraged.
- Plugin-facing public APIs added to Lib still live under the existing `LenovoLegionToolkit.Lib*` trees until Phase 2/3.

### Phase 2 — TypeForwardedTo / dual-package strategy for public types

When the ecosystem is ready to introduce `UniversalDeviceToolkit.Lib*` type names **without** a hard cutover:

- Ship type forwards (or a thin facade assembly) so old and new names resolve during a transition window.
- Publish coordinated SDK / plugin template updates (`using` and package references).
- Document which types are public ABI vs internal implementation.
- Avoid partial renames that only update the host.

### Phase 3 — Assembly rename after plugin ecosystem ready

- Change `AssemblyName` / primary namespace only after:

  - Official plugins recompiled against the new contract (or consume TypeForwardedTo packages),
  - Third-party guidance and a min-host / min-SDK version policy exist,
  - Installer/update story covers mixed old/new plugin folders if needed.

- Until then, treat any PR that only renames Lib assemblies as **breaking** and out of scope.

---

## CLI and IPC compatibility notes

| Concern | Current value | Guidance |
| --- | --- | --- |
| CLI `AssemblyName` | `llt` (`UniversalDeviceToolkit.CLI`) | Keep `llt.exe` for scripts and docs that invoke the short name; CrossPlatform shipping uses `udt` where appropriate. |
| Named pipe (host ↔ CLI IPC) — **legacy primary** | `LenovoLegionToolkit-IPC-0` (`Constants.DEFAULT_PIPE_NAME`) | **Server primary** listen name for full backward compatibility with older CLI / tooling that only knows the LLT pipe. |
| Named pipe (host ↔ CLI IPC) — **preferred UDT** | `UniversalDeviceToolkit-IPC-0` (`Constants.PREFERRED_PIPE_NAME`) | **Client-preferred** name. Host dual-listens on both; clients try UDT first, then fall back to legacy DEFAULT within a short timeout. Isolation-path hashing suffixes **both** names the same way. |
| Automation env vars | `LLT_*` (documented in README) | User scripts depend on these names; treat as compatibility surface, not dead branding. |
| Network proxy pipe | `udt-network-proxy` (and session-suffixed variants) | UDT-native; separate from CLI IPC. |

Phase 2 dual-pipe behavior (non-breaking):

- **Server (`IpcServer`)**: accept loops on both `DEFAULT_PIPE_NAME` and `PREFERRED_PIPE_NAME` (see `Constants.GetServerPipeNames`).
- **Client (`IpcClient`)**: `GetClientPipeNames` order — preferred UDT, then legacy LLT.
- Do not remove the legacy pipe constant or `llt` assembly name in drive-by cleanups until a hard cutover is deliberately planned.

---

## Conventions for contributors and agents

1. **User-visible brand** → Universal Device Toolkit / UDT.
2. **Plugin ABI assemblies** → keep `LenovoLegionToolkit.Lib` and `LenovoLegionToolkit.Lib.Plugins`.
3. **New non-ABI code** → `UniversalDeviceToolkit.*` namespaces.
4. **No mass search-replace** of namespaces across Lib.
5. **Before any ABI rename**: open a design note covering TypeForwardedTo (or dual package), plugins repo coordination, and release sequencing (Phases 2–3).

---

## Quick reference: intentional dual naming

```
Repo / solution:     UniversalDeviceToolkit.*
WPF process:         "Universal Device Toolkit"
Core Lib DLL:        LenovoLegionToolkit.Lib.dll
Plugins host DLL:    LenovoLegionToolkit.Lib.Plugins.dll
Plugin SDK (other):  LenovoLegionToolkit.Plugins.SDK / Shared
CLI exe (Windows):  llt.exe
CLI IPC pipes:       UniversalDeviceToolkit-IPC-0 (preferred) + LenovoLegionToolkit-IPC-0 (legacy primary)
NetworkProxy:        UniversalDeviceToolkit.NetworkProxy (fully UDT)
```
