# Language Packs

UDT Online ships an English-only host. Non-English UI is delivered as culture satellite packs from the online resource catalog.

## Catalog protocol

Catalog URL defaults to `AppIdentity.StableResourceCatalogUrl` (override with `UDT_RESOURCE_CATALOG_URL`).

Each language entry:

| Field | Purpose |
|---|---|
| `culture` | Culture name (e.g. `zh-hans`) |
| `parent` | Optional parent culture for fallback lookup |
| `size` | Package size in bytes |
| `sha256` | Content hash |
| `resourceVersion` | Pack revision independent of app version |
| `minAppVersion` | Minimum host app version that may install the pack |
| `url` | Download URL |
| `displayName` | Human-readable label |

## Service API (`LanguagePackManager`)

- `QueryCatalogAsync` — list available packs
- `InstallAsync` / `RepairAsync` / `UpdateAsync` — download → verify SHA256 → stage → validate satellite assemblies → atomic replace culture directories
- `Uninstall` / `QueueUninstall` / `ProcessPendingUninstall` — remove culture dirs (pending uninstall survives restart when the culture is active)
- Progress via `IProgress<float>`; cancellation via `CancellationToken`
- Structured failures: `LanguagePackException` + `LanguagePackFailureKind`

Fallback: if the language zip fails, extract matching culture directories from the full portable release zip.

## Culture resolution (non-Chinese UI strings)

Resource lookup uses exact culture → parent chain → English. Never falls back to Chinese for non-Chinese UI cultures.

## Startup language gate

After single-instance + bootstrap logging, and **before** IoC plugin/hardware init and `MainWindow` creation:

1. Show `LanguageSelectorWindow` when no saved language or device-setup is incomplete
2. Download failure presents **Retry / Continue in English / Exit** (never silent auto-English close)
3. Cancel/close cancels in-flight download, cleans temp dirs, returns `LanguageGateOutcome.Exit`
4. Safe-start / offline mode may continue in English without requiring a pack download

Settings-page installs use in-page progress through `LanguagePackInstallCoordinator` (no orphan top-level install window).

## Shipping footprint

Online packages must not ship non-English satellite assemblies. `Scripts/Prune-ShippingFootprint.ps1` removes disallowed `*.resources.dll` culture folders; allowed cultures are listed in `Directory.Build.props` (`UdtSatelliteResourceLanguages`).
