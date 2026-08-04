# Plugin Build Smoke

Use this checklist when you need a quick confidence pass before packaging or publishing plugins.

## Environment

```powershell
.\udt-plugin.cmd doctor
```

If host references are missing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\ensure-host-dependencies.ps1
```

## Fast Checks

Build a single plugin:

```powershell
.\udt-plugin.cmd build --plugin custom-mouse
```

Run that plugin's tests:

```powershell
.\udt-plugin.cmd test --plugin custom-mouse
```

Validate contributor requirements:

```powershell
.\udt-plugin.cmd validate --plugin custom-mouse --profile contributor
```

Create a local ZIP:

```powershell
.\udt-plugin.cmd package --plugin custom-mouse --build-first
```

## Official Candidate Check

```powershell
.\udt-plugin.cmd validate --plugin custom-mouse --profile official-candidate
```

Before writing root `store.json` for a selected release set, build the ZIPs first and require them:

```powershell
.\udt-plugin.cmd generate-store `
  --plugin-ids custom-mouse `
  --asset-root .\Build\release-assets `
  --merge-existing `
  --require-assets `
  --check
```

## Common Failures

- Missing .NET SDK: install the .NET 10 SDK.
- Missing Windows Desktop workload: install or repair the Windows Desktop workload.
- Missing host files: run `Scripts\ensure-host-dependencies.ps1`.
- Missing release asset: run `udt-plugin.cmd package --plugin <id> --build-first` before `generate-store --require-assets`.
