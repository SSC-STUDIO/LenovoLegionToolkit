# Agent rules (repository root)

Product: **Universal Device Toolkit** (UDT) — a Windows-first desktop app with an
Electron UI (`UniversalDeviceToolkit.Electron`) and a headless .NET 10 Host
(`UniversalDeviceToolkit.Host`). See `README.md`, `CONTRIBUTING.md`,
`Docs/ARCHITECTURE.md`, and `Docs/TEST_DIAGNOSTICS.md` for the full picture.

Default git branch: **`master`**. Prefer Conventional Commits. Do not force-push
unless explicitly asked. See `Plugins/AGENTS.md` for plugin-specific rules.

## Cursor Cloud specific instructions

The Cloud VM runs **Linux**, but the shipping product (full solution, `Host`,
official plugins) is **Windows-only** (`net10.0-windows10.0.26100.0`). On Linux
only the cross-platform surface builds/runs. Toolchains (.NET 10 SDK under
`~/.dotnet`, Node via nvm) are already installed by the environment; the startup
update script only refreshes dependencies.

### Node version gotcha (important)
The base image's `node` on `PATH` is `/exec-daemon/node` (v22.14), which is **too
old** for the Electron package's `node --test` runner — that runner imports
`.ts`/`.tsx` targets directly and needs Node **>= 22.18** (default TypeScript type
stripping). Interactive shells are fixed via `~/.bashrc` to prefer the
nvm-managed Node (v22.22, `lts/jod`); confirm with `node --version` before running
`npm test`/`npm run dev`. If you ever see many `ERR_UNKNOWN_FILE_EXTENSION ".ts"`
test failures, you are on the wrong Node.

### What runs on Linux (the supported cross-platform dev surface)
- **`udt` diagnostics CLI** — the runnable app on Linux. Build once
  (`dotnet build UniversalDeviceToolkit.CrossPlatform/UniversalDeviceToolkit.CrossPlatform.csproj -c Release`)
  then `dotnet run --project UniversalDeviceToolkit.CrossPlatform/UniversalDeviceToolkit.CrossPlatform.csproj -c Release --no-build -- <status|hardware|telemetry|power|profile|plugins|controls|support|doctor|json>`.
  It reads real host telemetry; `doctor` may exit 1 when it flags warnings.
- **Cross-platform tests** — build/test only the `CrossPlatform.Tests` project
  (it transitively builds the CLI lib). The exact commands live in
  `.github/workflows/linux.yml` and `.github/workflows/CrossPlatformCli.yml`.
  Do **not** `dotnet restore`/`build` the whole solution on Linux — the Windows
  WPF/WinForms projects fail with NETSDK1100.
- **Electron UI** — `npm run dev` (electron-vite dev + Electron window; a display
  is available at `DISPLAY=:1`), `npm run lint`, `npm run typecheck`, `npm test`
  from `UniversalDeviceToolkit.Electron`. If Electron fails with
  `Error: Electron uninstall`, the postinstall binary download was skipped/flaked;
  fix with `node node_modules/electron/install.js` (or re-run `npm ci`).

### Non-obvious Linux caveats
- **No .NET Host on Linux.** `scripts/ensure-host.mjs` is a no-op on non-Windows,
  and the *portable* Host currently does not compile from `master`
  (`Host/Rpc/Handlers/GameBoostHandlers.cs` references `GameBoost*` /
  `GameDetection` types not present in the `UDTWindows=false` `Lib` build). So
  `npm run dev` launches the Electron shell but the renderer has **no backend** —
  it lands on the Dashboard and shows a degraded/error state. Use it for UI shell
  work only; use the `udt` CLI for real device diagnostics on Linux.
- `dotnet` is under `~/.dotnet` (on `PATH` for interactive shells via `~/.bashrc`;
  the update script calls it by full path).
