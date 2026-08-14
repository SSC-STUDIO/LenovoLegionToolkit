# Universal Device Toolkit Plugin Quick Start

Shortest path for creating a plugin in this repository.

**Minimum supported host for 2.x official plugins:** Universal Device Toolkit **v6.0.0+** (preview tags `v6.0.0-preview.N`). Shipped stable host **v5.0.2** still loads 1.x from `plugin-catalog`. Vendored compile baseline **v5.0.2** until a v6 ZIP exists.<br>
**CLI:** `udt-plugin.cmd` (`udt-plugin.cmd` is a compatibility alias)

## 1. Check the environment

```powershell
.\udt-plugin.cmd doctor
```

If host references are missing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\ensure-host-dependencies.ps1
```

This uses `Plugins/HostBaseline/host-release.json` (currently `5.0.2`) and downloads the matching host binaries into the ignored `.host/` cache.

## 2. Scaffold

Templates:

- `settings-only`
- `feature-settings`
- `runtime-optimization`

```powershell
.\udt-plugin.cmd `
  init `
  --template feature-settings `
  --folder MyPlugin `
  --id my-plugin `
  --name "My Plugin"
```

Generates:

- `Plugins/Official/MyPlugin/`
- `Plugins/Official/MyPlugin.Tests/`
- `plugin.manifest.json` (authoring source of truth)
- `plugin.json` (host runtime compatibility output)
- plugin `CHANGELOG.md`
- resources + test project

## 3. Build

```powershell
.\udt-plugin.cmd build --plugin my-plugin
```

## 4. Preview in the Electron shell

Add `contributes.webPage.entry` (`web/index.html`), copy `web/` into the plugin output, then run the Electron app (`npm run dev` / `npm run dev:web`) and open the plugin from Plugins. The page calls `window.pluginHost.invoke`.

Workbench / WPF preview hosts are retired.

## 5. Validate (contributor)

```powershell
.\udt-plugin.cmd validate --plugin my-plugin --profile contributor
```

## 6. Package a local ZIP

```powershell
.\udt-plugin.cmd package --plugin my-plugin --build-first
```

## 7. Official store candidates only

```powershell
.\udt-plugin.cmd promote --plugin my-plugin
.\udt-plugin.cmd validate --plugin my-plugin --profile official-candidate
```

## 8. Do not start from generated `Plugins/.build/catalog/store.json`

Author flow:

1. Edit `plugin.manifest.json`
2. Implement plugin
3. Preview / test / validate
4. Package

Generated `Plugins/.build/catalog/store.json` is **release output** for active marketplace plugins only.
